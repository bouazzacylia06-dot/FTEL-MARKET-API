using FtelMarket.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FtelMarket.Api.Tests;

/// <summary>
/// Démarre l'API en mémoire sur une base PostgreSQL dédiée aux tests
/// (<c>ftelmarket_tests</c>), recréée à chaque exécution : la base de
/// développement n'est jamais touchée.
/// </summary>
public sealed class FtelMarketApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=ftelmarket_tests;Username=ftelmarket;Password=ftelmarket";

    /// <summary>Surchargeable par la variable d'environnement <c>FTELMARKET_TEST_DB</c>.</summary>
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("FTELMARKET_TEST_DB") ?? DefaultConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:FtelMarket", ConnectionString);
        // Les échecs sont rapportés par les assertions : les logs de l'API
        // (dont la tentative de connexion à une base pas encore créée) ne
        // feraient que bruiter la sortie des tests.
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FtelMarketDbContext>();

        // Base neuve : migrations appliquées puis seeding (UseAsyncSeeding).
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

/// <summary>
/// Tous les tests partagent la même base : ils s'exécutent séquentiellement.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<FtelMarketApiFactory>
{
    public const string Name = "API";
}
