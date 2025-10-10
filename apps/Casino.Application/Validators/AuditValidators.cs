using Casino.Application.DTOs.Audit;
using FluentValidation;

namespace Casino.Application.Validators;

public class QueryBackofficeAuditRequestValidator : AbstractValidator<QueryBackofficeAuditRequest>
{
    public QueryBackofficeAuditRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.Action)
            .MaximumLength(100)
            .WithMessage("Action cannot exceed 100 characters");

        RuleFor(x => x.TargetType)
            .MaximumLength(100)
            .WithMessage("TargetType cannot exceed 100 characters");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be less than or equal to ToDate");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .When(x => x.ToDate.HasValue)
            .WithMessage("ToDate cannot be in the future");
    }
}

public class QueryProviderAuditRequestValidator : AbstractValidator<QueryProviderAuditRequest>
{
    public QueryProviderAuditRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.Provider)
            .MaximumLength(50)
            .WithMessage("Provider cannot exceed 50 characters");

        RuleFor(x => x.Action)
            .MaximumLength(100)
            .WithMessage("Action cannot exceed 100 characters");

        RuleFor(x => x.SessionId)
            .MaximumLength(200)
            .WithMessage("SessionId cannot exceed 200 characters");

        RuleFor(x => x.PlayerId)
            .MaximumLength(200)
            .WithMessage("PlayerId cannot exceed 200 characters");

        RuleFor(x => x.GameCode)
            .MaximumLength(100)
            .WithMessage("GameCode cannot exceed 100 characters");

        RuleFor(x => x.RoundId)
            .MaximumLength(200)
            .WithMessage("RoundId cannot exceed 200 characters");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be less than or equal to ToDate");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .When(x => x.ToDate.HasValue)
            .WithMessage("ToDate cannot be in the future");
    }
}

public class ValidateSiteSetupRequestValidator : AbstractValidator<ValidateSiteSetupRequest>
{
    public ValidateSiteSetupRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.BrandCode) || !string.IsNullOrEmpty(x.Domain))
            .WithMessage("Either BrandCode or Domain must be provided");

        RuleFor(x => x.BrandCode)
            .Matches(@"^[A-Z0-9_]{2,20}$")
            .When(x => !string.IsNullOrEmpty(x.BrandCode))
            .WithMessage("BrandCode must be 2-20 characters long and contain only uppercase letters, numbers, and underscores");

        RuleFor(x => x.Domain)
            .Matches(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$")
            .When(x => !string.IsNullOrEmpty(x.Domain))
            .WithMessage("Domain must be a valid domain name");
    }
}