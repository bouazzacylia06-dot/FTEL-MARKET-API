using FtelMarket.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FtelMarket.Api.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(NameMaxLength);

        builder.Property(product => product.Description)
            .HasMaxLength(DescriptionMaxLength);

        // numeric(10,2) : montant exact, au centime, jusqu'à 99 999 999,99.
        builder.Property(product => product.Price)
            .HasPrecision(10, 2);

        builder.Property(product => product.CreatedAt)
            .HasColumnType("timestamp with time zone");

        // Filet de sécurité en base : la validation de l'API ne doit pas être
        // la seule barrière contre un prix nul ou un stock négatif.
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_products_price_positive", "price > 0");
            table.HasCheckConstraint("ck_products_stock_not_negative", "stock >= 0");
        });
    }
}
