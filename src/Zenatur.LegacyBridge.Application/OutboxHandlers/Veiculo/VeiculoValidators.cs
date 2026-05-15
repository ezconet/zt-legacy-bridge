using FluentValidation;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Veiculo;

public sealed class VeiculoEnvelopeValidator : AbstractValidator<VeiculoEnvelopePayload>
{
    public VeiculoEnvelopeValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.MessageType).NotEmpty();
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.Veiculo).NotNull();
        RuleFor(x => x.Veiculo.Placa)
            .NotEmpty()
            .Length(7)
            .WithMessage("Placa must be 7 chars (Mercosul or antigo).");
    }
}

public sealed class VeiculoRemovidoValidator : AbstractValidator<VeiculoRemovidoPayload>
{
    public VeiculoRemovidoValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.Placa).NotEmpty().Length(7);
    }
}
