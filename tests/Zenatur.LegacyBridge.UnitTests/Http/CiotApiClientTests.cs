using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Contrib.HttpClient;
using Polly;
using Polly.Retry;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Infrastructure.Http;

namespace Zenatur.LegacyBridge.UnitTests.Http;

public class CiotApiClientTests
{
    // No-retry pipeline keeps tests fast (default would wait 1+2+4 seconds).
    private static readonly ResiliencePipeline<HttpResponseMessage> NoRetryPipeline =
        ResiliencePipeline<HttpResponseMessage>.Empty;

    // Retry pipeline mirrors production behavior but skips delay between attempts.
    private static readonly ResiliencePipeline<HttpResponseMessage> InstantRetryPipeline =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false,
            })
            .Build();

    private static CiotApiClient Build(
        Mock<HttpMessageHandler> handler,
        ResiliencePipeline<HttpResponseMessage>? pipeline = null)
    {
        var http = handler.CreateClient();
        http.BaseAddress = new Uri("http://test.local");
        return new CiotApiClient(http, NullLogger<CiotApiClient>.Instance, pipeline ?? NoRetryPipeline);
    }

    [Fact]
    public async Task GetPending_Returns_Ok_With_Deserialised_List_On_200()
    {
        var handler = new Mock<HttpMessageHandler>();
        var json = """
            [{"id":1,"messageType":"Ciot.Emitido","payload":"{}","createdAt":"2026-05-12T19:00:00Z","retryCount":0}]
            """;
        handler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, json, "application/json");
        var client = Build(handler);

        var result = await client.GetPendingAsync(50, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(1, result.Value[0].Id);
        Assert.Equal("Ciot.Emitido", result.Value[0].MessageType);
    }

    [Fact]
    public async Task GetPending_Returns_Empty_On_200_Empty_Array()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");
        var client = Build(handler);

        var result = await client.GetPendingAsync(50, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetPending_Returns_Fail_On_401()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.Unauthorized);
        var client = Build(handler);

        var result = await client.GetPendingAsync(50, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
        Assert.Contains("401", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetPending_Returns_Fail_On_HttpRequestException_After_Retry_Exhaustion()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ThrowsAsync(new HttpRequestException("connection refused"));
        var client = Build(handler, InstantRetryPipeline);

        var result = await client.GetPendingAsync(50, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
        // 1 initial + 3 retries
        handler.VerifyAnyRequest(Times.Exactly(4));
    }

    [Fact]
    public async Task GetPending_Returns_Fail_On_500_After_Retry_Exhaustion()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.InternalServerError);
        var client = Build(handler, InstantRetryPipeline);

        var result = await client.GetPendingAsync(50, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
        Assert.Contains("500", result.Errors[0].Message);
        handler.VerifyAnyRequest(Times.Exactly(4));
    }

    [Fact]
    public async Task Ack_Returns_Ok_On_200()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.OK);
        var client = Build(handler);

        var result = await client.AckAsync(new AckRequest(42, true, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Ack_Returns_Fail_On_404()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.NotFound);
        var client = Build(handler);

        var result = await client.AckAsync(new AckRequest(99, false, "boom"), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
        Assert.Contains("404", result.Errors[0].Message);
    }

    [Fact]
    public async Task Ack_Posts_Json_Body_With_Camel_Case_Properties()
    {
        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.SetupAnyRequest()
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.Content is not null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync();
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        var client = Build(handler);

        await client.AckAsync(new AckRequest(7, false, "err"), CancellationToken.None);

        Assert.NotNull(capturedBody);
        Assert.Contains("\"id\":7", capturedBody);
        Assert.Contains("\"success\":false", capturedBody);
        Assert.Contains("\"errorLog\":\"err\"", capturedBody);
    }
}
