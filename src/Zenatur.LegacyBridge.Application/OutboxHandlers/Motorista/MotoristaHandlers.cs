using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Motorista;

public sealed class MotoristaCriadoHandler : OutboxHandlerBase<MotoristaEnvelopePayload>
{
    public MotoristaCriadoHandler(
        ILogger<MotoristaCriadoHandler> logger,
        IValidator<MotoristaEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Motorista.Criado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, MotoristaEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: UPSERT MOTORISTAS_CONDUTORES legado por (ContratanteCnpj, Cpf)
        Logger.LogInformation(
            "MotoristaCriado parsed+validated OutboxId {OutboxId} contratante={Contratante} cpf={Cpf} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, PiiMask.Cpf(payload.Motorista.Cpf));
        return Task.FromResult(Result.Ok());
    }
}

public sealed class MotoristaAtualizadoHandler : OutboxHandlerBase<MotoristaEnvelopePayload>
{
    public MotoristaAtualizadoHandler(
        ILogger<MotoristaAtualizadoHandler> logger,
        IValidator<MotoristaEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Motorista.Atualizado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, MotoristaEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: UPDATE MOTORISTAS_CONDUTORES legado
        Logger.LogInformation(
            "MotoristaAtualizado parsed+validated OutboxId {OutboxId} contratante={Contratante} cpf={Cpf} ativo={Ativo} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, PiiMask.Cpf(payload.Motorista.Cpf), payload.Motorista.Ativo);
        return Task.FromResult(Result.Ok());
    }
}
