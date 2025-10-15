using Casino.Application.DTOs.Wallet;
using Casino.Domain.Enums;
using FluentValidation;

namespace Casino.Application.Validators;

public class CreateAdminTransactionRequestValidator : AbstractValidator<CreateAdminTransactionRequest>
{
    public CreateAdminTransactionRequestValidator()
    {
        // FromUserId es opcional (null para MINT)
        RuleFor(x => x.FromUserType)
            .Must((request, fromUserType) => 
                !request.FromUserId.HasValue || 
                (fromUserType == "BACKOFFICE" || fromUserType == "PLAYER"))
            .WithMessage("FromUserType must be BACKOFFICE or PLAYER when FromUserId is provided");

        // ToUserId siempre requerido
        RuleFor(x => x.ToUserId)
            .NotEmpty()
            .WithMessage("ToUserId is required");

        RuleFor(x => x.ToUserType)
            .NotEmpty()
            .Must(type => type == "BACKOFFICE" || type == "PLAYER")
            .WithMessage("ToUserType must be BACKOFFICE or PLAYER");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Amount cannot exceed 1,000,000");

        RuleFor(x => x.TransactionType)
            .IsInEnum()
            .WithMessage("TransactionType must be valid");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("IdempotencyKey is required")
            .MaximumLength(100)
            .WithMessage("IdempotencyKey cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");
    }
}

public class GetAdminTransactionsRequestValidator : AbstractValidator<GetAdminTransactionsRequest>
{
    public GetAdminTransactionsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.TransactionType)
            .IsInEnum()
            .When(x => x.TransactionType.HasValue)
            .WithMessage("TransactionType must be valid");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be less than or equal to ToDate");

        RuleFor(x => x.ExternalRef)
            .MaximumLength(100)
            .WithMessage("ExternalRef cannot exceed 100 characters");
    }
}

public class AdminRollbackRequestValidator : AbstractValidator<AdminRollbackRequest>
{
    public AdminRollbackRequestValidator()
    {
        RuleFor(x => x.ExternalRef)
            .NotEmpty()
            .WithMessage("ExternalRef is required")
            .MaximumLength(100)
            .WithMessage("ExternalRef cannot exceed 100 characters");
    }
}