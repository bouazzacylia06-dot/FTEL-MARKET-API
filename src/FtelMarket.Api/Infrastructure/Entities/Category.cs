namespace FtelMarket.Api.Infrastructure.Entities;

/// <summary>Catégorie de rangement des produits (ex. « PlayStation 5 »).</summary>
public class Category
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Une catégorie regroupe plusieurs produits.</summary>
    public ICollection<Product> Products { get; set; } = [];
}
