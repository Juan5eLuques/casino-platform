using Casino.Application.DTOs.Operator;
using Casino.Domain.Enums;
using FluentValidation;

namespace Casino.Application.Validators.Operator;

public class CreateOperatorRequestValidator : AbstractValidator<CreateOperatorRequest>
{
    public CreateOperatorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 100)
            .WithMessage("Name must be between 1 and 100 characters");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid status value");
    }
}

public class UpdateOperatorRequestValidator : AbstractValidator<UpdateOperatorRequest>
{
    public UpdateOperatorRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .Length(1, 100)
                .WithMessage("Name must be between 1 and 100 characters");
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value");
        });
    }
}

public class QueryOperatorsRequestValidator : AbstractValidator<QueryOperatorsRequest>
{
    public QueryOperatorsRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .Length(1, 100)
                .WithMessage("Name filter must be between 1 and 100 characters");
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