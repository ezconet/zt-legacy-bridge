using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Veiculo;

public sealed class VeiculoCriadoHandler : OutboxHandlerBase<VeiculoEnvelopePayload>
{
    public VeiculoCriadoHandler(
        ILogger<VeiculoCriadoHandler> logger,
        IValidator<VeiculoEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Veiculo.Criado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, VeiculoEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: UPSERT VEICULOS legado por (ContratanteCnpj, Placa)
        Logger.LogInformation(
            "VeiculoCriado parsed+validated OutboxId {OutboxId} contratante={Contratante} placa={Placa} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, payload.Veiculo.Placa);
        return Task.FromResult(Result.Ok());
    }
}

public sealed class VeiculoAtualizadoHandler : OutboxHandlerBase<VeiculoEnvelopePayload>
{
    public VeiculoAtualizadoHandler(
        ILogger<VeiculoAtualizadoHandler> logger,
        IValidator<VeiculoEnvelopePayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Veiculo.Atualizado";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, VeiculoEnvelopePayload payload, CancellationToken ct)
    {
        // TODO B6: UPDATE VEICULOS legado
        Logger.LogInformation(
            "VeiculoAtualizado parsed+validated OutboxId {OutboxId} contratante={Contratante} placa={Placa} ativo={Ativo} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, payload.Veiculo.Placa, payload.Veiculo.Ativo);
        return Task.FromResult(Result.Ok());
    }
}

public sealed class VeiculoRemovidoHandler : OutboxHandlerBase<VeiculoRemovidoPayload>
{
    public VeiculoRemovidoHandler(
        ILogger<VeiculoRemovidoHandler> logger,
        IValidator<VeiculoRemovidoPayload> validator)
        : base(logger, validator) { }

    public override string MessageType => "Veiculo.Removido";

    protected override Task<Result> ProcessAsync(
        OutboxMessageDto message, VeiculoRemovidoPayload payload, CancellationToken ct)
    {
        // TODO B6: UPDATE Ativo=false em VEICULOS legado
        Logger.LogInformation(
            "VeiculoRemovido parsed+validated OutboxId {OutboxId} contratante={Contratante} placa={Placa} — legacy persist pending B6",
            message.Id, payload.ContratanteCnpj, payload.Placa);
        return Task.FromResult(Result.Ok());
    }
}
