using Casino.Application.DTOs.Player;
using Casino.Domain.Enums;
using FluentValidation;

namespace Casino.Application.Validators.Player;

public class CreatePlayerRequestValidator : AbstractValidator<CreatePlayerRequest>
{
    public CreatePlayerRequestValidator()
    {
        // NOTA: Removida validación de BrandId requerido - ahora se resuelve automáticamente en el endpoint
        // El endpoint valida que BrandId esté presente después de resolver automáticamente desde brandContext

        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username must be 3-50 characters and contain only letters, numbers, dots, hyphens and underscores");

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .Length(5, 100)
                .WithMessage("Email must be a valid email address between 5 and 100 characters");
        });

        When(x => !string.IsNullOrEmpty(x.ExternalId), () =>
        {
            RuleFor(x => x.ExternalId)
                .Length(1, 100)
                .WithMessage("ExternalId must be between 1 and 100 characters");
        });

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Initial balance cannot be negative");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid status value");
    }
}

public class UpdatePlayerRequestValidator : AbstractValidator<UpdatePlayerRequest>
{
    public UpdatePlayerRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Username), () =>
        {
            RuleFor(x => x.Username)
                .Length(3, 50)
                .Matches("^[a-zA-Z0-9_.-]+$")
                .WithMessage("Username must be 3-50 characters and contain only letters, numbers, dots, hyphens and underscores");
        });

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .Length(5, 100)
                .WithMessage("Email must be a valid email address between 5 and 100 characters");
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value");
        });
    }
}

public class QueryPlayersRequestValidator : AbstractValidator<QueryPlayersRequest>
{
    public QueryPlayersRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Username), () =>
        {
            RuleFor(x => x.Username)
                .Length(1, 50)
                .WithMessage("Username filter must be between 1 and 50 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .Length(1, 100)
                .WithMessage("Email filter must be between 1 and 100 characters");
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

public class AdjustPlayerWalletRequestValidator : AbstractValidator<AdjustPlayerWalletRequest>
{
    public AdjustPlayerWalletRequestValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0)
            .WithMessage("Amount cannot be zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .Length(1, 200)
            .WithMessage("Reason is required and must be between 1 and 200 characters");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .Length(1, 500)
                .WithMessage("Description must be between 1 and 500 characters");
        });
    }
}