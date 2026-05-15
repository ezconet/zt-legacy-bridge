using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Composicao;

public sealed class ComposicaoCriadaHandler : OutboxHandlerBase<ComposicaoEnvelopePayload>
{
    public ComposicaoCriadaHandler(
        ILogger<ComposicaoCriadaHandler> logger,
        IValidator<ComposicaoEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Composicao.Criada";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, ComposicaoEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: INSERT COMPOSICOES + COMPOSICOES_REBOQUES (N reboques)
        Logger.LogInformation(
            "ComposicaoCriada parsed+validated OutboxId {OutboxId} contratante={Contratante} compId={CompId} tracionante={Placa} reboques={Reboques} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, payload.Composicao.Id,
            payload.Composicao.TracionantePlaca, payload.Composicao.Reboques.Count);
        return Task.FromResult(Result.Ok());
    }
}

public sealed class ComposicaoAtualizadaHandler : OutboxHandlerBase<ComposicaoEnvelopePayload>
{
    public ComposicaoAtualizadaHandler(
        ILogger<ComposicaoAtualizadaHandler> logger,
        IValidator<ComposicaoEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Composicao.Atualizada";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, ComposicaoEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: UPDATE COMPOSICOES + DELETE/re-INSERT COMPOSICOES_REBOQUES
        Logger.LogInformation(
            "ComposicaoAtualizada parsed+validated OutboxId {OutboxId} contratante={Contratante} compId={CompId} reboques={Reboques} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, payload.Composicao.Id, payload.Composicao.Reboques.Count);
        return Task.FromResult(Result.Ok());
    }
}

public sealed class ComposicaoRemovidaHandler : OutboxHandlerBase<ComposicaoRemovidaPayload>
{
    public ComposicaoRemovidaHandler(
        ILogger<ComposicaoRemovidaHandler> logger,
        IValidator<ComposicaoRemovidaPayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Composicao.Removida";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, ComposicaoRemovidaPayload payload, CancellationToken ct)
    {
        // TODO B6: UPDATE Ativo=false em COMPOSICOES (reboques ficam — histórico)
        Logger.LogInformation(
            "ComposicaoRemovida parsed+validated OutboxId {OutboxId} contratante={Contratante} compId={CompId} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, payload.ComposicaoId);
        return Task.FromResult(Result.Ok());
    }
}
