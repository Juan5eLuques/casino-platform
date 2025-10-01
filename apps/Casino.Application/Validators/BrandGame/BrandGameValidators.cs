using Casino.Application.DTOs.BrandGame;
using FluentValidation;

namespace Casino.Application.Validators.BrandGame;

public class AssignGameToBrandRequestValidator : AbstractValidator<AssignGameToBrandRequest>
{
    public AssignGameToBrandRequestValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("GameId is required");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DisplayOrder cannot be negative");

        When(x => x.Tags != null && x.Tags.Length > 0, () =>
        {
            RuleFor(x => x.Tags!)
                .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
                .WithMessage("All tags must be non-empty");

            RuleFor(x => x.Tags!)
                .Must(tags => tags.Length <= 20)
                .WithMessage("Cannot have more than 20 tags");

            RuleForEach(x => x.Tags!)
                .Length(1, 50)
                .WithMessage("Each tag must be between 1 and 50 characters");
        });
    }
}

public class UpdateBrandGameRequestValidator : AbstractValidator<UpdateBrandGameRequest>
{
    public UpdateBrandGameRequestValidator()
    {
        When(x => x.DisplayOrder.HasValue, () =>
        {
            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("DisplayOrder cannot be negative");
        });

        When(x => x.Tags != null && x.Tags.Length > 0, () =>
        {
            RuleFor(x => x.Tags!)
                .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
                .WithMessage("All tags must be non-empty");

            RuleFor(x => x.Tags!)
                .Must(tags => tags.Length <= 20)
                .WithMessage("Cannot have more than 20 tags");

            RuleForEach(x => x.Tags!)
                .Length(1, 50)
                .WithMessage("Each tag must be between 1 and 50 characters");
        });
    }
}