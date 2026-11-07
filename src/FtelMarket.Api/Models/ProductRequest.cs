using System.ComponentModel.DataAnnotations;
using FtelMarket.Api.Infrastructure.Configurations;

namespace FtelMarket.Api.Models;

/// <summary>
/// Corps de <c>POST /api/products</c> et <c>PUT /api/products/{id}</c>
/// (<c>zProductPayload</c> dans <c>schemas.ts</c>).
/// </summary>
/// <remarks>
/// Les champs obligatoires de type valeur sont nullables : sans cela un champ
/// absent du JSON vaudrait silencieusement 0 au lieu d'être signalé manquant.
/// En cas d'erreur, <c>[ApiController]</c> répond 400 au format ProblemDetails.
/// </remarks>
public sealed class ProductRequest : IValidatableObject
{
    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(ProductConfiguration.NameMaxLength, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères.")]
    public string? Name { get; init; }

    [StringLength(ProductConfiguration.DescriptionMaxLength, ErrorMessage = "La description ne peut pas dépasser 1000 caractères.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "Le prix est obligatoire.")]
    [Range(typeof(decimal), "0.01", "1000000",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Le prix doit être compris entre 0,01 et 1 000 000.")]
    public decimal? Price { get; init; }

    [Required(ErrorMessage = "Le stock est obligatoire.")]
    [Range(0, int.MaxValue, ErrorMessage = "Le stock ne peut pas être négatif.")]
    public int? Stock { get; init; }

    [Required(ErrorMessage = "La catégorie est obligatoire.")]
    [Range(1, int.MaxValue, ErrorMessage = "La catégorie est obligatoire.")]
    public int? CategoryId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Un prix se compte en centimes : 19.999 serait arrondi en base sans prévenir.
        if (Price is { } price && decimal.Round(price, 2) != price)
        {
            yield return new ValidationResult(
                "Le prix ne peut pas avoir plus de 2 décimales.", [nameof(Price)]);
        }
    }
}
