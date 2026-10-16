namespace FtelMarket.Api.Models;

/// <summary>
/// Produit tel qu'attendu par le front (<c>zProduct</c> dans <c>schemas.ts</c>).
/// </summary>
/// <param name="CategoryName">Dénormalisé : le front l'affiche sans second appel.</param>
/// <param name="CreatedAt">UTC, sérialisé en ISO 8601 avec le suffixe <c>Z</c>.</param>
public sealed record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt);
