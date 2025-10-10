using Casino.Application.DTOs.UnifiedUser;
using Casino.Domain.Enums;
using FluentValidation;

namespace Casino.Application.Validators.UnifiedUser;

/// <summary>
/// SONNET: Validadores para endpoints unificados de usuarios
/// </summary>
public class CreateUnifiedUserRequestValidator : AbstractValidator<CreateUnifiedUserRequest>
{
    public CreateUnifiedUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
            .Matches("^[a-zA-Z0-9_.-]+$").WithMessage("Username can only contain letters, numbers, dots, hyphens and underscores");

        // SONNET: Password es requerido para backoffice users
        RuleFor(x => x.Password)
            .NotEmpty().When(x => x.Role.HasValue && x.Role.Value != BackofficeUserRole.PLAYER)
            .WithMessage("Password is required for backoffice users")
            .MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password must be at least 8 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.CommissionPercent)
            .InclusiveBetween(0, 100).WithMessage("Commission percent must be between 0 and 100")
            .Equal(0).When(x => x.Role.HasValue && x.Role.Value != BackofficeUserRole.CASHIER)
            .WithMessage("Only CASHIER role can have commission percent");

        RuleFor(x => x.ParentCashierId)
            .Null().When(x => !x.Role.HasValue || x.Role.Value != BackofficeUserRole.CASHIER)
            .WithMessage("Only CASHIER role can have a parent cashier");
    }
}

public class UpdateUnifiedUserRequestValidator : AbstractValidator<UpdateUnifiedUserRequest>
{
    public UpdateUnifiedUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(3).When(x => !string.IsNullOrEmpty(x.Username))
            .WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.Username))
            .WithMessage("Username must not exceed 50 characters")
            .Matches("^[a-zA-Z0-9_.-]+$").When(x => !string.IsNullOrEmpty(x.Username))
            .WithMessage("Username can only contain letters, numbers, dots, hyphens and underscores");

        RuleFor(x => x.Password)
            .MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password must be at least 8 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.CommissionPercent)
            .InclusiveBetween(0, 100).When(x => x.CommissionPercent.HasValue)
            .WithMessage("Commission percent must be between 0 and 100");
    }
}
