using System.Net;
using System.Net.Http.Json;
using FtelMarket.Api.Models;

namespace FtelMarket.Api.Tests;

[Collection(ApiCollection.Name)]
public class CategoriesApiTests(FtelMarketApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_returns_seeded_categories_ordered_by_id()
    {
        var response = await _client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.NotNull(categories);
        Assert.Equal(
            ["PlayStation 5", "Nintendo Switch", "Xbox Series X", "PC"],
            categories.Select(category => category.Name));
        Assert.Equal(categories.OrderBy(category => category.Id), categories);
    }

    [Fact]
    public async Task GetAll_uses_camelCase_field_names()
    {
        var json = await _client.GetStringAsync("/api/categories");

        Assert.Contains("\"id\":", json);
        Assert.Contains("\"name\":", json);
    }
}
