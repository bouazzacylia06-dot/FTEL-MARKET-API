namespace FtelMarket.Api.Infrastructure.Entities;

/// <summary>Produit mis en vente dans le catalogue d'un utilisateur.</summary>
public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Facultative.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// <see cref="decimal"/> et non <see cref="double"/>/<see cref="float"/> :
    /// les flottants binaires ne représentent pas exactement 0,10 ou 19,90,
    /// ce qui produit des erreurs d'arrondi inacceptables sur de l'argent.
    /// </summary>
    public decimal Price { get; set; }

    public int Stock { get; set; }

    /// <summary>
    /// Instant de création, toujours en UTC (<c>timestamp with time zone</c>).
    /// La conversion vers le fuseau de l'utilisateur est faite à l'affichage.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Un produit est rangé dans une, et une seule, catégorie.</summary>
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    /// <summary>Utilisateur dont le catalogue contient ce produit.</summary>
    public int SellerId { get; set; }

    public User Seller { get; set; } = null!;
}
