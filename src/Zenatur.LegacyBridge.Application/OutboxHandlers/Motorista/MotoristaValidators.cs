using FluentValidation;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Motorista;

public sealed class MotoristaEnvelopeValidator : AbstractValidator<MotoristaEnvelopePayload>
{
    public MotoristaEnvelopeValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.MessageType).NotEmpty();
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.Motorista).NotNull();
        RuleFor(x => x.Motorista.Cpf)
            .NotEmpty()
            .Length(11)
            .WithMessage("Motorista CPF must be 11 digits.");
        RuleFor(x => x.Motorista.Nome).NotEmpty();
    }
}
