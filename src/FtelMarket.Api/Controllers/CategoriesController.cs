using FtelMarket.Api.Infrastructure;
using FtelMarket.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FtelMarket.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(FtelMarketDbContext dbContext) : ControllerBase
{
    /// <summary>Liste toutes les catégories (alimente la liste déroulante du formulaire).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CategoryResponse>> GetAll(CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Id)
            .Select(category => new CategoryResponse(category.Id, category.Name))
            .ToListAsync(cancellationToken);
    }
}
