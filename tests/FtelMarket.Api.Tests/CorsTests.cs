using System.Net;

namespace FtelMarket.Api.Tests;

[Collection(ApiCollection.Name)]
public class CorsTests(FtelMarketApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("PUT", "/api/products/1")]
    [InlineData("POST", "/api/products")]
    [InlineData("DELETE", "/api/products/1")]
    public async Task Preflight_from_front_origin_is_allowed(string method, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:3000", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains(method, string.Join(',', response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task Other_origins_are_not_allowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
        request.Headers.Add("Origin", "http://evil.example");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
