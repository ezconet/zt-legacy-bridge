using System.Text.Json;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

public abstract class OutboxHandlerBase<TPayload> : IOutboxMessageHandler
    where TPayload : class
{
    private readonly IValidator<TPayload>? _validator;

    protected OutboxHandlerBase(ILogger logger, IValidator<TPayload>? validator = null)
    {
        Logger = logger;
        _validator = validator;
    }

    protected ILogger Logger { get; }

    public abstract string MessageType { get; }

    public virtual int SupportedSchemaVersion => 1;

    public async Task<Result> HandleAsync(OutboxMessageDto message, CancellationToken ct)
    {
        TPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<TPayload>(message.Payload, OutboxJsonOptions.Default)
                      ?? throw new InvalidOperationException("Payload deserialised to null.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "PayloadDeserializationError OutboxId {OutboxId} type {MessageType}",
                message.Id, MessageType);
            return Result.Fail(new ValidationError(
                $"Failed to deserialize {MessageType} payload: {ex.Message}"));
        }

        if (_validator is not null)
        {
            var vresult = await _validator.ValidateAsync(payload, ct);
            if (!vresult.IsValid)
            {
                var errors = string.Join("; ", vresult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
                Logger.LogWarning(
                    "PayloadValidationFailed OutboxId {OutboxId} type {MessageType}: {Errors}",
                    message.Id, MessageType, errors);
                return Result.Fail(new ValidationError(errors));
            }
        }

        return await ProcessAsync(message, payload, ct);
    }

    protected abstract Task<Result> ProcessAsync(OutboxMessageDto message, TPayload payload, CancellationToken ct);
}
