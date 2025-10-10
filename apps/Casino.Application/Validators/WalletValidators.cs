using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using FluentValidation;

namespace Casino.Application.Validators;

// Validadores para DTOs de wallet legado (compatibilidad con gateway)
public class WalletDebitRequestValidator : AbstractValidator<WalletDebitRequest>
{
    public WalletDebitRequestValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(100).WithMessage("Reason cannot exceed 100 characters");

        RuleFor(x => x.ExternalRef)
            .MaximumLength(100).WithMessage("ExternalRef cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ExternalRef));
    }
}

public class WalletCreditRequestValidator : AbstractValidator<WalletCreditRequest>
{
    public WalletCreditRequestValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(100).WithMessage("Reason cannot exceed 100 characters");

        RuleFor(x => x.ExternalRef)
            .MaximumLength(100).WithMessage("ExternalRef cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ExternalRef));
    }
}

public class WalletRollbackRequestValidator : AbstractValidator<WalletRollbackRequest>
{
    public WalletRollbackRequestValidator()
    {
        RuleFor(x => x.ExternalRefOriginal)
            .NotEmpty().WithMessage("ExternalRefOriginal is required")
            .MaximumLength(100).WithMessage("ExternalRefOriginal cannot exceed 100 characters");
    }
}

// Los validadores de wallet unificado ya están en WalletValidators.cs:
// - TransferWalletRequestValidator
// - AdjustWalletRequestValidator  
// - GetWalletsRequestValidator
// - GetWalletLedgerRequestValidator