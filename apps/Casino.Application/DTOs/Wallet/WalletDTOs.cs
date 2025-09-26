using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Wallet;

public record BalanceRequest(Guid PlayerId);

public record BalanceResponse(long Balance);

public record DebitRequest(
    Guid PlayerId,
    long Amount,
    LedgerReason Reason,
    Guid? RoundId,
    string ExternalRef,
    string? GameCode = null,
    string? Provider = null);

public record CreditRequest(
    Guid PlayerId,
    long Amount,
    LedgerReason Reason,
    Guid? RoundId,
    string ExternalRef,
    string? GameCode = null,
    string? Provider = null);

public record WalletOperationResponse(
    bool Success,
    long Balance,
    long? LedgerId = null,
    string? ErrorMessage = null);

public record RollbackRequest(string ExternalRefOriginal);