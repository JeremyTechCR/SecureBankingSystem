using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SecureBanking.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyServiceStatus()
    {
        using HttpResponseMessage response = await _client.GetAsync("/health", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        HealthResponse? health = await response.Content.ReadFromJsonAsync<HealthResponse>(
            cancellationToken: CancellationToken.None);

        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal("SecureBankingSystem", health.Service);
        Assert.InRange(health.TimestampUtc, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow);
    }

    private sealed record HealthResponse(string Status, string Service, DateTimeOffset TimestampUtc);
}
