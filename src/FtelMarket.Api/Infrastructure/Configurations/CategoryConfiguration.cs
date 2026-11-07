using FtelMarket.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FtelMarket.Api.Infrastructure.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public const int NameMaxLength = 100;

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(NameMaxLength);

        // Deux catégories ne peuvent pas porter le même nom.
        builder.HasIndex(category => category.Name).IsUnique();

        // Impossible de supprimer une catégorie qui contient encore des produits.
        builder.HasMany(category => category.Products)
            .WithOne(product => product.Category)
            .HasForeignKey(product => product.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
