using Casino.Application.DTOs.Wallet;
using FluentValidation;

namespace Casino.Application.Validators;

/// <summary>
/// SONNET: Validador para transacciones simples con IdempotencyKey requerido
/// </summary>
public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.ToUserId)
            .NotEmpty().WithMessage("ToUserId is required");

        RuleFor(x => x.ToUserType)
            .NotEmpty().WithMessage("ToUserType is required")
            .Must(x => x == "BACKOFFICE" || x == "PLAYER")
            .WithMessage("ToUserType must be BACKOFFICE or PLAYER");

        RuleFor(x => x.FromUserType)
            .Must(x => x == null || x == "BACKOFFICE" || x == "PLAYER")
            .WithMessage("FromUserType must be null, BACKOFFICE or PLAYER")
            .When(x => !string.IsNullOrEmpty(x.FromUserType));

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive")
            .LessThanOrEqualTo(999999999.99m).WithMessage("Amount exceeds maximum allowed");

        // SONNET: IdempotencyKey es requerido
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required")
            .MaximumLength(100).WithMessage("IdempotencyKey cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Validar consistencia entre FromUserId y FromUserType
        RuleFor(x => x)
            .Must(HaveConsistentFromFields)
            .WithMessage("If FromUserId is provided, FromUserType must also be provided")
            .Must(NotBeSelfTransfer)
            .WithMessage("Cannot transfer to the same user");
    }

    private bool HaveConsistentFromFields(CreateTransactionRequest request)
    {
        // Si FromUserId está presente, FromUserType debe estar presente también
        if (request.FromUserId.HasValue && string.IsNullOrEmpty(request.FromUserType))
            return false;

        // Si FromUserType está presente, FromUserId debe estar presente también
        if (!string.IsNullOrEmpty(request.FromUserType) && !request.FromUserId.HasValue)
            return false;

        return true;
    }

    private bool NotBeSelfTransfer(CreateTransactionRequest request)
    {
        if (!request.FromUserId.HasValue) return true; // MINT operation

        return !(request.FromUserId.Value == request.ToUserId && 
                request.FromUserType == request.ToUserType);
    }
}

public class GetTransactionsRequestValidator : AbstractValidator<GetTransactionsRequest>
{
    public GetTransactionsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.UserType)
            .Must(x => x == null || x == "BACKOFFICE" || x == "PLAYER")
            .WithMessage("UserType must be null, BACKOFFICE or PLAYER")
            .When(x => !string.IsNullOrEmpty(x.UserType));

        RuleFor(x => x)
            .Must(HaveValidDateRange)
            .WithMessage("FromDate must be less than or equal to ToDate")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description filter cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }

    private bool HaveValidDateRange(GetTransactionsRequest request)
    {
        return !request.FromDate.HasValue || !request.ToDate.HasValue || 
               request.FromDate.Value <= request.ToDate.Value;
    }
}