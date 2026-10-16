using System.Linq.Expressions;
using FtelMarket.Api.Infrastructure;
using FtelMarket.Api.Infrastructure.Entities;
using FtelMarket.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FtelMarket.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(FtelMarketDbContext dbContext, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Projection traduite en SQL : seules les colonnes utiles sont lues, et le
    /// nom de catégorie vient d'une jointure plutôt que d'un second appel.
    /// </summary>
    private static readonly Expression<Func<Product, ProductResponse>> ToResponse = product =>
        new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CategoryId,
            product.Category.Name,
            product.CreatedAt);

    /// <summary>Liste tout le catalogue.</summary>
    /// <remarks>
    /// <c>page</c> et <c>pageSize</c>, envoyés par le front, sont volontairement
    /// ignorés dans ce TP : la pagination côté serveur viendra plus tard.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ProductResponse>> GetAll(CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Lit un produit.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await FindResponseAsync(id, cancellationToken);

        return product is null ? ProductNotFound(id) : product;
    }

    /// <summary>Ajoute un produit au catalogue.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        if (!await CategoryExistsAsync(request.CategoryId!.Value, cancellationToken))
        {
            return CategoryNotFound(request.CategoryId.Value);
        }

        // Pas encore d'authentification : le produit rejoint le catalogue de
        // l'utilisateur de démonstration. C'est ici que viendra l'utilisateur connecté.
        var sellerId = await dbContext.Users
            .Where(user => user.UserName == DataSeeder.DemoUserName)
            .Select(user => user.Id)
            .SingleAsync(cancellationToken);

        var product = new Product
        {
            Name = request.Name!.Trim(),
            SellerId = sellerId,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        Apply(request, product);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await FindResponseAsync(product.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    /// <summary>Remplace intégralement un produit (tous les champs sont exigés).</summary>
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([id], cancellationToken);

        if (product is null)
        {
            return ProductNotFound(id);
        }

        if (!await CategoryExistsAsync(request.CategoryId!.Value, cancellationToken))
        {
            return CategoryNotFound(request.CategoryId.Value);
        }

        Apply(request, product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await FindResponseAsync(id, cancellationToken))!;
    }

    /// <summary>Retire un produit du catalogue.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        // Un seul DELETE SQL, sans charger l'entité au préalable.
        var deletedCount = await dbContext.Products
            .Where(product => product.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount == 0 ? ProductNotFound(id) : NoContent();
    }

    /// <summary>Copie les champs modifiables ; la validation a déjà garanti les valeurs requises.</summary>
    private static void Apply(ProductRequest request, Product product)
    {
        product.Name = request.Name!.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price!.Value;
        product.Stock = request.Stock!.Value;
        product.CategoryId = request.CategoryId!.Value;
    }

    private Task<ProductResponse?> FindResponseAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(ToResponse)
            .SingleOrDefaultAsync(cancellationToken);

    private Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);

    private NotFoundObjectResult ProductNotFound(int id) =>
        NotFound(new ErrorResponse($"Le produit {id} n'existe pas."));

    private BadRequestObjectResult CategoryNotFound(int categoryId) =>
        BadRequest(new ErrorResponse($"La catégorie {categoryId} n'existe pas."));
}
