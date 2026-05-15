using FluentValidation;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido;

public sealed class FavorecidoEnvelopeValidator : AbstractValidator<FavorecidoEnvelopePayload>
{
    public FavorecidoEnvelopeValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.MessageType).NotEmpty();
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.Favorecido).NotNull();
        RuleFor(x => x.Favorecido.Documento)
            .NotEmpty()
            .Must(d => d.Length is 11 or 14)
            .WithMessage("Documento must be CPF (11) or CNPJ (14) digits.");
        RuleFor(x => x.Favorecido.Nome).NotEmpty();
    }
}

public sealed class ContaEventValidator : AbstractValidator<ContaEventPayload>
{
    public ContaEventValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.FavorecidoDocumento).NotEmpty();
        RuleFor(x => x.Conta).NotNull();
        RuleFor(x => x.Conta.Banco).GreaterThan(0);
        RuleFor(x => x.Conta.Agencia).NotEmpty();
        RuleFor(x => x.Conta.Numero).NotEmpty();
    }
}

public sealed class ContaRemovidaValidator : AbstractValidator<ContaRemovidaPayload>
{
    public ContaRemovidaValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.FavorecidoDocumento).NotEmpty();
        RuleFor(x => x.ContaId).GreaterThan(0);
    }
}
