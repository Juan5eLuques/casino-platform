using FluentValidation;
using Casino.Application.DTOs.Wallet;

namespace Casino.Application.Validators;

public class DebitRequestValidator : AbstractValidator<DebitRequest>
{
    public DebitRequestValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ExternalRef).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.GameCode).MaximumLength(100).When(x => x.GameCode != null);
        RuleFor(x => x.Provider).MaximumLength(100).When(x => x.Provider != null);
    }
}

public class CreditRequestValidator : AbstractValidator<CreditRequest>
{
    public CreditRequestValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ExternalRef).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.GameCode).MaximumLength(100).When(x => x.GameCode != null);
        RuleFor(x => x.Provider).MaximumLength(100).When(x => x.Provider != null);
    }
}

public class RollbackRequestValidator : AbstractValidator<RollbackRequest>
{
    public RollbackRequestValidator()
    {
        RuleFor(x => x.ExternalRefOriginal).NotEmpty().MaximumLength(255);
    }
}