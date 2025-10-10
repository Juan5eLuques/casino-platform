using Casino.Application.DTOs.Admin;
using FluentValidation;

namespace Casino.Application.Validators.Admin;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters");

        When(x => !string.IsNullOrEmpty(x.CurrentPassword), () =>
        {
            RuleFor(x => x.CurrentPassword)
                .MinimumLength(1).WithMessage("Current password cannot be empty when provided");
        });
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters");
    }
}