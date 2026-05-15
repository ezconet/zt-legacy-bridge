using FluentValidation;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Vinculo;

public sealed class VinculoValidator : AbstractValidator<VinculoPayload>
{
    public VinculoValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.MessageType).NotEmpty();
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.FavorecidoDocumento).NotEmpty();
        RuleFor(x => x.MotoristaCpf)
            .NotEmpty()
            .Length(11)
            .WithMessage("Motorista CPF must be 11 digits.");
    }
}
