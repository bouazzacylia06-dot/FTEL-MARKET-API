namespace FtelMarket.Api.Models;

/// <summary>Catégorie telle qu'exposée par <c>GET /api/categories</c>.</summary>
public sealed record CategoryResponse(int Id, string Name);
