using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Dispatching;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.UnitTests.Dispatching;

public class OutboxDispatcherTests
{
    private static OutboxMessageDto Msg(string type, string payload, long id = 1) =>
        new(id, type, payload, DateTime.UtcNow, 0);

    private static Mock<IOutboxMessageHandler> MockHandler(
        string type, int version, Result? result = null)
    {
        var m = new Mock<IOutboxMessageHandler>();
        m.SetupGet(h => h.MessageType).Returns(type);
        m.SetupGet(h => h.SupportedSchemaVersion).Returns(version);
        m.Setup(h => h.HandleAsync(It.IsAny<OutboxMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? Result.Ok());
        return m;
    }

    private static OutboxDispatcher BuildDispatcher(params IOutboxMessageHandler[] handlers) =>
        new(handlers, NullLogger<OutboxDispatcher>.Instance);

    [Fact]
    public async Task Returns_Fail_When_No_Handler_For_MessageType()
    {
        var dispatcher = BuildDispatcher();

        var result = await dispatcher.DispatchAsync(
            Msg("Unknown.Type", "{\"schemaVersion\":1}"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
        Assert.Contains("No handler", result.Errors[0].Message);
    }

    [Fact]
    public async Task Returns_Fail_When_Payload_Not_Json()
    {
        var handler = MockHandler("Ciot.Emitido", 1);
        var dispatcher = BuildDispatcher(handler.Object);

        var result = await dispatcher.DispatchAsync(
            Msg("Ciot.Emitido", "<<not json>>"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
        handler.Verify(h => h.HandleAsync(It.IsAny<OutboxMessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Returns_Fail_When_SchemaVersion_Missing()
    {
        var handler = MockHandler("Ciot.Emitido", 1);
        var dispatcher = BuildDispatcher(handler.Object);

        var result = await dispatcher.DispatchAsync(
            Msg("Ciot.Emitido", "{\"foo\":\"bar\"}"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
        Assert.Contains("schemaVersion", result.Errors[0].Message);
    }

    [Fact]
    public async Task Returns_Fail_When_SchemaVersion_Mismatches()
    {
        var handler = MockHandler("Ciot.Emitido", 1);
        var dispatcher = BuildDispatcher(handler.Object);

        var result = await dispatcher.DispatchAsync(
            Msg("Ciot.Emitido", "{\"schemaVersion\":2}"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
        Assert.Contains("schema v1", result.Errors[0].Message);
        handler.Verify(h => h.HandleAsync(It.IsAny<OutboxMessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Calls_Handler_When_Type_And_Version_Match()
    {
        var handler = MockHandler("Ciot.Emitido", 1);
        var dispatcher = BuildDispatcher(handler.Object);

        var result = await dispatcher.DispatchAsync(
            Msg("Ciot.Emitido", "{\"schemaVersion\":1,\"protocoloCiot\":\"X\"}"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        handler.Verify(h => h.HandleAsync(It.IsAny<OutboxMessageDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Propagates_Handler_Failure()
    {
        var handler = MockHandler("Ciot.Emitido", 1, Result.Fail(new InfraError("DbDown")));
        var dispatcher = BuildDispatcher(handler.Object);

        var result = await dispatcher.DispatchAsync(
            Msg("Ciot.Emitido", "{\"schemaVersion\":1}"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<InfraError>(result.Errors[0]);
    }
}
