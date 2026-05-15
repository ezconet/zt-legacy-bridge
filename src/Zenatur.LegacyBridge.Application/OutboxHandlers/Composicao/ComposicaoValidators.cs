using FluentValidation;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Composicao;

public sealed class ComposicaoEnvelopeValidator : AbstractValidator<ComposicaoEnvelopePayload>
{
    public ComposicaoEnvelopeValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.MessageType).NotEmpty();
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.Composicao).NotNull();
        RuleFor(x => x.Composicao.TracionantePlaca).NotEmpty();
        RuleFor(x => x.Composicao.Reboques).NotNull();
    }
}

public sealed class ComposicaoRemovidaValidator : AbstractValidator<ComposicaoRemovidaPayload>
{
    public ComposicaoRemovidaValidator()
    {
        RuleFor(x => x.SchemaVersion).Equal(1);
        RuleFor(x => x.ContratanteCnpj).NotEmpty().Length(14);
        RuleFor(x => x.ComposicaoId).GreaterThan(0);
    }
}
