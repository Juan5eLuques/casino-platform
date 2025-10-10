using Casino.Application.DTOs.Admin;
using Casino.Domain.Enums;
using FluentValidation;

namespace Casino.Application.Validators.Admin;

public class CreateBackofficeUserRequestValidator : AbstractValidator<CreateBackofficeUserRequest>
{
    public CreateBackofficeUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username must be 3-50 characters and contain only letters, numbers, dots, hyphens and underscores");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Invalid role value");

        RuleFor(x => x.CommissionRate)
            .InclusiveBetween(0, 100)
            .WithMessage("Commission rate must be between 0 and 100");

        RuleFor(x => x)
            .Must(x => x.Role == BackofficeUserRole.CASHIER || (!x.ParentCashierId.HasValue && x.CommissionRate == 0))
            .WithMessage("Only CASHIER role can have ParentCashierId and CommissionRate");
    }
}

public class UpdateBackofficeUserRequestValidator : AbstractValidator<UpdateBackofficeUserRequest>
{
    public UpdateBackofficeUserRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Username), () =>
        {
            RuleFor(x => x.Username)
                .Length(3, 50)
                .Matches("^[a-zA-Z0-9_.-]+$")
                .WithMessage("Username must be 3-50 characters and contain only letters, numbers, dots, hyphens and underscores");
        });

        When(x => !string.IsNullOrEmpty(x.Password), () =>
        {
            RuleFor(x => x.Password)
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long");
        });

        When(x => x.Role.HasValue, () =>
        {
            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid role value");
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value");
        });

        When(x => x.CommissionRate.HasValue, () =>
        {
            RuleFor(x => x.CommissionRate)
                .InclusiveBetween(0, 100)
                .WithMessage("Commission rate must be between 0 and 100");
        });
    }
}

public class QueryBackofficeUsersRequestValidator : AbstractValidator<QueryBackofficeUsersRequest>
{
    public QueryBackofficeUsersRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Username), () =>
        {
            RuleFor(x => x.Username)
                .Length(1, 50)
                .WithMessage("Username filter must be between 1 and 50 characters");
        });

        When(x => x.Role.HasValue, () =>
        {
            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid role filter value");
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status filter value");
        });

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");
    }
}