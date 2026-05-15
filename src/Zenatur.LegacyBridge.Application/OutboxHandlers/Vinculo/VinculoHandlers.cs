using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Vinculo;

public sealed class FavorecidoMotoristaVinculadoHandler : OutboxHandlerBase<VinculoPayload>
{
    public FavorecidoMotoristaVinculadoHandler(
        ILogger<FavorecidoMotoristaVinculadoHandler> logger,
        IValidator<VinculoPayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "FavorecidoMotorista.Vinculado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, VinculoPayload payload, CancellationToken ct)
    {
        // TODO B6: INSERT LEGACY_FAVORECIDO_MOTORISTA_LINK (idempotente, chave composta)
        Logger.LogInformation(
            "FavorecidoMotoristaVinculado parsed+validated OutboxId {OutboxId} fav={Fav} cpf={Cpf} — legacy persist pending B6",
            message.Id, PiiMask.Documento(payload.FavorecidoDocumento), PiiMask.Cpf(payload.MotoristaCpf));
        return Task.FromResult(Result.Ok());
    }
}

public sealed class FavorecidoMotoristaDesvinculadoHandler : OutboxHandlerBase<VinculoPayload>
{
    public FavorecidoMotoristaDesvinculadoHandler(
        ILogger<FavorecidoMotoristaDesvinculadoHandler> logger,
        IValidator<VinculoPayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "FavorecidoMotorista.Desvinculado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, VinculoPayload payload, CancellationToken ct)
    {
        // TODO B6: UPDATE DesvinculadoEm = now() no link existente
        Logger.LogInformation(
            "FavorecidoMotoristaDesvinculado parsed+validated OutboxId {OutboxId} fav={Fav} cpf={Cpf} — legacy persist pending B6",
            message.Id, PiiMask.Documento(payload.FavorecidoDocumento), PiiMask.Cpf(payload.MotoristaCpf));
        return Task.FromResult(Result.Ok());
    }
}
