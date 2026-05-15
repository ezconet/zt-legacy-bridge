using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido;

public sealed class FavorecidoCriadoHandler : OutboxHandlerBase<FavorecidoEnvelopePayload>
{
    public FavorecidoCriadoHandler(
        ILogger<FavorecidoCriadoHandler> logger,
        IValidator<FavorecidoEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Favorecido.Criado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, FavorecidoEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: UPSERT FAVORECIDOS legado por (ContratanteCnpj, Documento)
        Logger.LogInformation(
            "FavorecidoCriado parsed+validated OutboxId {OutboxId} contratante={Contratante} doc={Doc} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, PiiMask.Documento(payload.Favorecido.Documento));
        return Task.FromResult(Result.Ok());
    }
}

public sealed class FavorecidoAtualizadoHandler : OutboxHandlerBase<FavorecidoEnvelopePayload>
{
    public FavorecidoAtualizadoHandler(
        ILogger<FavorecidoAtualizadoHandler> logger,
        IValidator<FavorecidoEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Favorecido.Atualizado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, FavorecidoEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: UPDATE FAVORECIDOS legado; ativo=false → soft-delete
        Logger.LogInformation(
            "FavorecidoAtualizado parsed+validated OutboxId {OutboxId} contratante={Contratante} doc={Doc} ativo={Ativo} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, PiiMask.Documento(payload.Favorecido.Documento), payload.Favorecido.Ativo);
        return Task.FromResult(Result.Ok());
    }
}

public sealed class FavorecidoContaAdicionadaHandler : OutboxHandlerBase<ContaEventPayload>
{
    public FavorecidoContaAdicionadaHandler(
        ILogger<FavorecidoContaAdicionadaHandler> logger,
        IValidator<ContaEventPayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Favorecido.ContaAdicionada";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, ContaEventPayload payload, CancellationToken ct)
    {
        // TODO B6: INSERT CONTAS_BANCARIAS legado
        Logger.LogInformation(
            "FavorecidoContaAdicionada parsed+validated OutboxId {OutboxId} doc={Doc} contaId={ContaId} banco={Banco} — legacy persist pending B6",
            message.Id, PiiMask.Documento(payload.FavorecidoDocumento), payload.Conta.Id, payload.Conta.Banco);
        return Task.FromResult(Result.Ok());
    }
}

public sealed class FavorecidoContaAtualizadaHandler : OutboxHandlerBase<ContaEventPayload>
{
    public FavorecidoContaAtualizadaHandler(
        ILogger<FavorecidoContaAtualizadaHandler> logger,
        IValidator<ContaEventPayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Favorecido.ContaAtualizada";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, ContaEventPayload payload, CancellationToken ct)
    {
        // TODO B6: UPSERT CONTAS_BANCARIAS por (favorecidoDocumento, banco, agencia, numero)
        Logger.LogInformation(
            "FavorecidoContaAtualizada parsed+validated OutboxId {OutboxId} doc={Doc} contaId={ContaId} — legacy persist pending B6",
            message.Id, PiiMask.Documento(payload.FavorecidoDocumento), payload.Conta.Id);
        return Task.FromResult(Result.Ok());
    }
}

public sealed class FavorecidoContaRemovidaHandler : OutboxHandlerBase<ContaRemovidaPayload>
{
    public FavorecidoContaRemovidaHandler(
        ILogger<FavorecidoContaRemovidaHandler> logger,
        IValidator<ContaRemovidaPayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Favorecido.ContaRemovida";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, ContaRemovidaPayload payload, CancellationToken ct)
    {
        // TODO B6: soft-delete CONTAS_BANCARIAS
        Logger.LogInformation(
            "FavorecidoContaRemovida parsed+validated OutboxId {OutboxId} doc={Doc} contaId={ContaId} — legacy persist pending B6",
            message.Id, PiiMask.Documento(payload.FavorecidoDocumento), payload.ContaId);
        return Task.FromResult(Result.Ok());
    }
}
