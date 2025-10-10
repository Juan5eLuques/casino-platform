using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Wallet;

/// <summary>
/// Response para operaciones de wallet (mantener compatibilidad)
/// </summary>
public record WalletOperationResponse(
    bool Success,
    Guid? OperationId,
    string Message,
    decimal? Balance = null, // Para compatibilidad con Gateway
    string? ErrorMessage = null // Para compatibilidad con Gateway
);

/// <summary>
/// Request para obtener balance de wallet
/// </summary>
public record WalletBalanceRequest(
    Guid PlayerId
);

/// <summary>
/// Response de balance de wallet
/// </summary>
public record WalletBalanceResponse(
    decimal Balance
);

/// <summary>
/// Request para d�bito de wallet (apuestas)
/// </summary>
public record WalletDebitRequest(
    Guid PlayerId,
    long Amount, // En centavos (bigint legacy)
    string Reason,
    string? GameRoundId = null,
    string? ExternalRef = null
);

/// <summary>
/// Request para cr�dito de wallet (ganancias)
/// </summary>
public record WalletCreditRequest(
    Guid PlayerId,
    long Amount, // En centavos (bigint legacy)
    string Reason,
    string? GameRoundId = null,
    string? ExternalRef = null
);

/// <summary>
/// Request para rollback de transacci�n
/// </summary>
public record WalletRollbackRequest(
    string ExternalRefOriginal
);