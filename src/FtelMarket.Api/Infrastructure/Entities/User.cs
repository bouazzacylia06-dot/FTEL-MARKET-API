namespace FtelMarket.Api.Infrastructure.Entities;

/// <summary>Utilisateur de la place de marché, propriétaire d'un catalogue de produits.</summary>
public class User
{
    public int Id { get; set; }

    public required string UserName { get; set; }

    /// <summary>Instant d'inscription, en UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Le catalogue de l'utilisateur.</summary>
    public ICollection<Product> Products { get; set; } = [];
}
