using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Wallet;

/// <summary>
/// Request para crear transacciones de admin que usa TransactionType enum
/// RESPETA la estructura de CreateTransactionRequest con FromUserId/ToUserId
/// Permite transferencias entre cualquier tipo de usuario (PLAYER/BACKOFFICE)
/// MINT: fromUserId=null, toUserId válido (crear dinero)
/// WITHDRAWAL: fromUserId válido, toUserId=null (quemar dinero)
/// TRANSFER: ambos válidos (transferir entre usuarios)
/// </summary>
public record CreateAdminTransactionRequest(
    Guid? FromUserId,           // null = MINT (crear dinero)
    string? FromUserType,       // null = MINT, "BACKOFFICE" | "PLAYER"
    Guid ToUserId,             // null = WITHDRAWAL (quemar dinero)
    string? ToUserType,         // null = WITHDRAWAL, "BACKOFFICE" | "PLAYER"
    decimal Amount,             // En decimal, no en centavos
    TransactionType TransactionType, // BET, WIN, TRANSFER, MINT, DEPOSIT, WITHDRAWAL, etc.
    string IdempotencyKey,      // REQUERIDO para idempotencia
    string? Description = null
);

/// <summary>
/// Response de transacción admin que respeta la estructura de SimpleWalletService
/// Mantiene compatibilidad con TransactionResponse pero agrega TransactionType
/// </summary>
public record AdminTransactionResponse(
    Guid Id,
    Guid BrandId,
    string Type, // "MINT" | "TRANSFER" | "BET" | "WIN" etc. - derivado de TransactionType
    Guid? FromUserId,
    string? FromUserType,
    string? FromUsername,
    decimal? PreviousBalanceFrom,
    decimal? NewBalanceFrom,
    Guid ToUserId,
    string ToUserType,
    string ToUsername,
    decimal PreviousBalanceTo,
    decimal NewBalanceTo,
    decimal Amount,
    string? Description,
    TransactionType TransactionType, // Campo adicional para filtrar por tipo
    Guid CreatedByUserId,
    string CreatedByUsername,
    string CreatedByRole,
    string IdempotencyKey,
    DateTime CreatedAt
);

/// <summary>
/// Request para listar transacciones admin con filtros
/// </summary>
public record GetAdminTransactionsRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    TransactionType? TransactionType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? ExternalRef = null,
    bool GlobalScope = false // Para SUPER_ADMIN
);

/// <summary>
/// Response paginado de transacciones admin
/// </summary>
public record GetAdminTransactionsResponse(
    IEnumerable<AdminTransactionResponse> Transactions,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// Request para rollback de transacciones admin
/// </summary>
public record AdminRollbackRequest(
    string ExternalRef // Referencia externa de la transacción a revertir
);