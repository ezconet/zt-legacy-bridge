using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Zenatur.LegacyBridge.IntegrationTests.Middleware;

public class ApiKeyMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidKey = "test-key-abc123";
    private const string SecuredPath = "/__test/secured-ping";

    private readonly WebApplicationFactory<Program> _factory;

    public ApiKeyMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InboundApiKey"] = ValidKey
                });
            });
        });
    }

    [Fact]
    public async Task Returns_401_When_Header_Missing()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(SecuredPath);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_401_When_Header_Wrong()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var resp = await client.GetAsync(SecuredPath);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_200_When_Header_Correct()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidKey);

        var resp = await client.GetAsync(SecuredPath);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("\"pong\"", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_Live_Bypasses_ApiKey()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
