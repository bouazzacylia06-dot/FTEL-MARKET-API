using FtelMarket.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FtelMarket.Api.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const int UserNameMaxLength = 50;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName)
            .IsRequired()
            .HasMaxLength(UserNameMaxLength);

        builder.HasIndex(user => user.UserName).IsUnique();

        builder.Property(user => user.CreatedAt)
            .HasColumnType("timestamp with time zone");

        // Supprimer un utilisateur supprime son catalogue.
        builder.HasMany(user => user.Products)
            .WithOne(product => product.Seller)
            .HasForeignKey(product => product.SellerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
