namespace Casino.Application.DTOs.Wallet;

/// <summary>
/// Request para crear una transacción/transferencia
/// SONNET: Incluye IdempotencyKey para prevenir duplicados
/// </summary>
public record CreateTransactionRequest(
    Guid? FromUserId,
    string? FromUserType, // null = MINT, "BACKOFFICE" | "PLAYER"
    Guid ToUserId,
    string ToUserType, // "BACKOFFICE" | "PLAYER"
    decimal Amount,
    string IdempotencyKey, // SONNET: Requerido para idempotencia
    string? Description = null
);

/// <summary>
/// Response de una transacción
/// SONNET: Incluye PreviousBalance/NewBalance para auditoría completa
/// </summary>
public record TransactionResponse(
    Guid Id,
    Guid BrandId, // SONNET: Incluir brandId en response
    string Type, // "MINT" | "TRANSFER"
    Guid? FromUserId,
    string? FromUserType,
    string? FromUsername,
    // SONNET: Balances del usuario origen (null para MINT)
    decimal? PreviousBalanceFrom,
    decimal? NewBalanceFrom,
    Guid ToUserId,
    string ToUserType,
    string ToUsername,
    // SONNET: Balances del usuario destino
    decimal PreviousBalanceTo,
    decimal NewBalanceTo,
    decimal Amount,
    string? Description,
    Guid CreatedByUserId,
    string CreatedByUsername,
    string CreatedByRole,
    string IdempotencyKey,
    DateTime CreatedAt
);

/// <summary>
/// Request para obtener transacciones con filtros
/// </summary>
public record GetTransactionsRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null, // Filtrar por usuario (from o to)
    string? UserType = null, // Filtrar por tipo de usuario
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Description = null,
    bool GlobalScope = false // SONNET: Para SUPER_ADMIN ver todas las brands
);

/// <summary>
/// Response paginado de transacciones
/// </summary>
public record GetTransactionsResponse(
    IEnumerable<TransactionResponse> Transactions,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// SONNET: Response de balance del sistema simple de wallet
/// Incluye información adicional de usuario y tipo
/// </summary>
public record SimpleWalletBalanceResponse(
    Guid UserId,
    string UserType, // "BACKOFFICE" | "PLAYER"
    string Username,
    decimal Balance
);

// WalletBalanceResponse movido a WalletDTOs.cs para evitar duplicados