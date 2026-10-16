using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FtelMarket.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FtelMarket.Api.Tests;

[Collection(ApiCollection.Name)]
public class ProductsApiTests(FtelMarketApiFactory factory)
{
    private const string Route = "/api/products";

    private readonly HttpClient _client = factory.CreateClient();

    private static object ValidPayload(int categoryId = 1) => new
    {
        name = "Elden Ring",
        description = "Comme neuf.",
        price = 34.90m,
        stock = 2,
        categoryId,
    };

    private async Task<ProductResponse> CreateProductAsync(object? payload = null)
    {
        var response = await _client.PostAsJsonAsync(Route, payload ?? ValidPayload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    // ---------- GET /api/products ----------

    [Fact]
    public async Task GetAll_returns_the_catalogue_with_category_names()
    {
        // Le front envoie page/pageSize : ils doivent être acceptés et ignorés.
        var response = await _client.GetAsync($"{Route}?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        Assert.True(products.Count >= 8);
        Assert.All(products, product => Assert.False(string.IsNullOrEmpty(product.CategoryName)));

        var zelda = Assert.Single(products, product => product.Name == "The Legend of Zelda: Tears of the Kingdom");
        Assert.Equal("Nintendo Switch", zelda.CategoryName);
        Assert.Equal(39.90m, zelda.Price);
    }

    [Fact]
    public async Task GetAll_serialises_dates_in_utc_iso8601()
    {
        var json = await _client.GetStringAsync(Route);

        Assert.Contains("\"createdAt\":\"2026-09-02T09:15:00Z\"", json);
    }

    [Fact]
    public async Task GetAll_exposes_null_description()
    {
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>(Route);

        Assert.Null(Assert.Single(products!, product => product.Name == "Gran Turismo 7").Description);
    }

    // ---------- GET /api/products/{id} ----------

    [Fact]
    public async Task GetById_returns_the_product()
    {
        var created = await CreateProductAsync();

        var product = await _client.GetFromJsonAsync<ProductResponse>($"{Route}/{created.Id}");

        Assert.Equal(created, product);
    }

    [Fact]
    public async Task GetById_unknown_returns_404_with_message()
    {
        var response = await _client.GetAsync($"{Route}/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Le produit 999999 n'existe pas.", error!.Message);
    }

    // ---------- POST /api/products ----------

    [Fact]
    public async Task Create_returns_201_with_location_and_product()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);

        var response = await _client.PostAsJsonAsync(Route, ValidPayload(categoryId: 4));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        Assert.True(product.Id > 8, "L'id doit être généré par la base, après les données initiales.");
        Assert.Equal("Elden Ring", product.Name);
        Assert.Equal("Comme neuf.", product.Description);
        Assert.Equal(34.90m, product.Price);
        Assert.Equal(2, product.Stock);
        Assert.Equal(4, product.CategoryId);
        Assert.Equal("PC", product.CategoryName);
        Assert.Equal(DateTimeKind.Utc, product.CreatedAt.Kind);
        Assert.InRange(product.CreatedAt, before, DateTime.UtcNow.AddSeconds(5));
        Assert.Equal($"/api/products/{product.Id}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Create_persists_the_product()
    {
        var created = await CreateProductAsync();

        var products = await _client.GetFromJsonAsync<List<ProductResponse>>(Route);

        Assert.Contains(created, products!);
    }

    [Fact]
    public async Task Create_trims_text_and_turns_blank_description_into_null()
    {
        var product = await CreateProductAsync(new
        {
            name = "  Hollow Knight  ",
            description = "   ",
            price = 9.99m,
            stock = 0,
            categoryId = 4,
        });

        Assert.Equal("Hollow Knight", product.Name);
        Assert.Null(product.Description);
    }

    [Fact]
    public async Task Create_keeps_exact_decimal_price()
    {
        // 0.1 + 0.2 vaut 0.30000000000000004 en double : pas en decimal.
        var product = await CreateProductAsync(new { name = "Prix exact", price = 0.1m + 0.2m, stock = 1, categoryId = 1 });

        Assert.Equal(0.30m, product.Price);
    }

    [Fact]
    public async Task Create_accepts_boundary_values()
    {
        var cheapest = await CreateProductAsync(new { name = "Min", price = 0.01m, stock = 0, categoryId = 1 });
        var mostExpensive = await CreateProductAsync(new
        {
            name = new string('a', 200),
            description = new string('b', 1000),
            price = 1_000_000m,
            stock = int.MaxValue,
            categoryId = 1,
        });

        Assert.Equal(0.01m, cheapest.Price);
        Assert.Equal(1_000_000m, mostExpensive.Price);
    }

    [Fact]
    public async Task Create_with_invalid_fields_returns_validation_problem_per_field()
    {
        var response = await _client.PostAsJsonAsync(Route, new { name = "", price = 0m, stock = -1, categoryId = 0 });

        var problem = await AssertValidationProblemAsync(response);
        Assert.Equal(["Le nom est obligatoire."], problem.Errors["Name"]);
        Assert.Equal(["Le prix doit être compris entre 0,01 et 1 000 000."], problem.Errors["Price"]);
        Assert.Equal(["Le stock ne peut pas être négatif."], problem.Errors["Stock"]);
        Assert.Equal(["La catégorie est obligatoire."], problem.Errors["CategoryId"]);
    }

    [Fact]
    public async Task Create_with_missing_fields_reports_each_one()
    {
        var response = await _client.PostAsJsonAsync(Route, new { });

        var problem = await AssertValidationProblemAsync(response);
        Assert.Equal(["Le nom est obligatoire."], problem.Errors["Name"]);
        Assert.Equal(["Le prix est obligatoire."], problem.Errors["Price"]);
        Assert.Equal(["Le stock est obligatoire."], problem.Errors["Stock"]);
        Assert.Equal(["La catégorie est obligatoire."], problem.Errors["CategoryId"]);
    }

    [Theory]
    [InlineData(201, 1000, "Name", "Le nom ne peut pas dépasser 200 caractères.")]
    [InlineData(10, 1001, "Description", "La description ne peut pas dépasser 1000 caractères.")]
    public async Task Create_rejects_too_long_text(int nameLength, int descriptionLength, string field, string message)
    {
        var response = await _client.PostAsJsonAsync(Route, new
        {
            name = new string('a', nameLength),
            description = new string('b', descriptionLength),
            price = 10m,
            stock = 1,
            categoryId = 1,
        });

        var problem = await AssertValidationProblemAsync(response);
        Assert.Equal([message], problem.Errors[field]);
    }

    [Fact]
    public async Task Create_rejects_price_above_maximum()
    {
        var response = await _client.PostAsJsonAsync(Route, new { name = "Trop cher", price = 1_000_000.01m, stock = 1, categoryId = 1 });

        var problem = await AssertValidationProblemAsync(response);
        Assert.Contains("Price", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_rejects_more_than_two_decimals()
    {
        var response = await _client.PostAsJsonAsync(Route, new { name = "Arrondi", price = 1.999m, stock = 1, categoryId = 1 });

        var problem = await AssertValidationProblemAsync(response);
        Assert.Equal(["Le prix ne peut pas avoir plus de 2 décimales."], problem.Errors["Price"]);
    }

    [Fact]
    public async Task Create_rejects_non_integer_stock()
    {
        var response = await _client.PostAsync(Route, JsonContent(
            """{ "name": "x", "price": 1, "stock": 1.5, "categoryId": 1 }"""));

        var problem = await AssertValidationProblemAsync(response);
        Assert.Contains("$.stock", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_with_unknown_category_returns_business_error()
    {
        var response = await _client.PostAsJsonAsync(Route, ValidPayload(categoryId: 999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("La catégorie 999 n'existe pas.", error!.Message);
    }

    // ---------- PUT /api/products/{id} ----------

    [Fact]
    public async Task Update_replaces_every_field_and_keeps_creation_date()
    {
        var created = await CreateProductAsync();

        var response = await _client.PutAsJsonAsync($"{Route}/{created.Id}", new
        {
            name = "Elden Ring - GOTY",
            description = (string?)null,
            price = 29.50m,
            stock = 7,
            categoryId = 3,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(
            created with
            {
                Name = "Elden Ring - GOTY",
                Description = null,
                Price = 29.50m,
                Stock = 7,
                CategoryId = 3,
                CategoryName = "Xbox Series X",
            },
            updated);
        Assert.Equal(updated, await _client.GetFromJsonAsync<ProductResponse>($"{Route}/{created.Id}"));
    }

    [Fact]
    public async Task Update_unknown_product_returns_404()
    {
        var response = await _client.PutAsJsonAsync($"{Route}/999999", ValidPayload());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Le produit 999999 n'existe pas.", error!.Message);
    }

    [Fact]
    public async Task Update_with_invalid_payload_returns_400_and_changes_nothing()
    {
        var created = await CreateProductAsync();

        var response = await _client.PutAsJsonAsync($"{Route}/{created.Id}", new { name = "", price = -5m, stock = 1, categoryId = 1 });

        await AssertValidationProblemAsync(response);
        Assert.Equal(created, await _client.GetFromJsonAsync<ProductResponse>($"{Route}/{created.Id}"));
    }

    [Fact]
    public async Task Update_with_unknown_category_returns_business_error()
    {
        var created = await CreateProductAsync();

        var response = await _client.PutAsJsonAsync($"{Route}/{created.Id}", ValidPayload(categoryId: 999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("La catégorie 999 n'existe pas.", error!.Message);
    }

    // ---------- DELETE /api/products/{id} ----------

    [Fact]
    public async Task Delete_returns_204_then_product_is_gone()
    {
        var created = await CreateProductAsync();

        var response = await _client.DeleteAsync($"{Route}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{Route}/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_twice_returns_404_the_second_time()
    {
        var created = await CreateProductAsync();
        await _client.DeleteAsync($"{Route}/{created.Id}");

        var response = await _client.DeleteAsync($"{Route}/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal($"Le produit {created.Id} n'existe pas.", error!.Message);
    }

    // ---------- Helpers ----------

    private static StringContent JsonContent(string json) =>
        new(json, System.Text.Encoding.UTF8, "application/json");

    private static async Task<ValidationProblemDetails> AssertValidationProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.NotEmpty(problem.Errors);
        return problem;
    }
}
