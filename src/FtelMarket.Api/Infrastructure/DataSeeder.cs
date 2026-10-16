using FtelMarket.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace FtelMarket.Api.Infrastructure;

/// <summary>
/// Données initiales, insérées par <c>dotnet ef database update</c> (ou
/// <c>Database.Migrate()</c>) via <c>UseSeeding</c>/<c>UseAsyncSeeding</c>.
/// </summary>
/// <remarks>
/// On n'utilise pas <c>HasData</c> : insérer des Id explicites ne fait pas
/// avancer les séquences PostgreSQL, et le premier POST échouerait sur une
/// violation de clé primaire. Ici les Id sont générés par la base.
/// Le seeding est idempotent : il ne fait rien si les tables sont déjà remplies.
/// </remarks>
public static class DataSeeder
{
    /// <summary>
    /// Propriétaire des produits tant que l'API n'a pas d'authentification.
    /// </summary>
    public const string DemoUserName = "demo";

    private static readonly string[] CategoryNames =
        ["PlayStation 5", "Nintendo Switch", "Xbox Series X", "PC"];

    private sealed record SeedProduct(
        string Name, string? Description, decimal Price, int Stock, string Category, DateTime CreatedAt);

    private static readonly SeedProduct[] Products =
    [
        new("The Last of Us Part II", "Boîte et notice, disque sans rayure.", 19.90m, 3, "PlayStation 5", Utc(2026, 9, 2, 9, 15)),
        new("Gran Turismo 7", null, 29.90m, 1, "PlayStation 5", Utc(2026, 9, 5, 14, 30)),
        new("The Legend of Zelda: Tears of the Kingdom", "Cartouche seule.", 39.90m, 2, "Nintendo Switch", Utc(2026, 9, 8, 10, 0)),
        new("Mario Kart 8 Deluxe", "Complet, très bon état.", 34.90m, 5, "Nintendo Switch", Utc(2026, 9, 10, 16, 45)),
        new("Halo Infinite", null, 14.90m, 4, "Xbox Series X", Utc(2026, 9, 12, 11, 20)),
        new("Forza Horizon 5", "Édition standard.", 24.90m, 0, "Xbox Series X", Utc(2026, 9, 15, 8, 5)),
        new("Baldur’s Gate 3", "Clé physique, coffret collector.", 44.90m, 1, "PC", Utc(2026, 9, 18, 17, 40)),
        new("Hades", null, 9.90m, 6, "PC", Utc(2026, 9, 21, 13, 10)),
    ];

    public static void Seed(DbContext context)
    {
        if (context.Set<Category>().Any())
        {
            return;
        }

        AddSeedData(context);
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<Category>().AnyAsync(cancellationToken))
        {
            return;
        }

        AddSeedData(context);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AddSeedData(DbContext context)
    {
        var demoUser = context.Set<User>().FirstOrDefault(user => user.UserName == DemoUserName)
            ?? new User { UserName = DemoUserName, CreatedAt = Utc(2026, 9, 1, 8, 0) };

        var categories = CategoryNames.ToDictionary(name => name, name => new Category { Name = name });

        context.Set<Category>().AddRange(categories.Values);
        context.Set<Product>().AddRange(Products.Select(seed => new Product
        {
            Name = seed.Name,
            Description = seed.Description,
            Price = seed.Price,
            Stock = seed.Stock,
            CreatedAt = seed.CreatedAt,
            Category = categories[seed.Category],
            Seller = demoUser,
        }));
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);
}
