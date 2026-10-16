namespace FtelMarket.Api.Models;

/// <summary>
/// Erreur métier (ressource introuvable, catégorie inexistante) : un simple
/// <c>{ "message": "..." }</c>, distinct du ProblemDetails des erreurs de saisie.
/// </summary>
public sealed record ErrorResponse(string Message);
