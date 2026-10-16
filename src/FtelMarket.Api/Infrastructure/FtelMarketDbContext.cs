using FtelMarket.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace FtelMarket.Api.Infrastructure;

public class FtelMarketDbContext(DbContextOptions<FtelMarketDbContext> options) : DbContext(options)
{
    // Déclaration des tables de la BDD
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FtelMarketDbContext).Assembly);
    }
}
