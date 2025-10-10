using Casino.Application.DTOs.Wallet;
using Casino.Domain.Enums;

namespace Casino.Application.Services;

/// <summary>
/// SONNET: Servicio de wallet simple con garantías críticas
/// Sistema SIMPLE+ con idempotencia, transacciones y scope por brand
/// </summary>
public interface ISimpleWalletService
{
    /// <summary>
    /// SONNET: Método simplificado para endpoint con CreateTransactionRequest
    /// </summary>
    Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, 
        Guid actorUserId, BackofficeUserRole actorRole, Guid brandId);

    /// <summary>
    /// SONNET: Transferir entre usuarios (MINT si fromUserId es null)
    /// Garantías: Idempotencia + Transacciones DB + Locking + Scope por brand
    /// </summary>
    Task<TransactionResponse> TransferAsync(
        Guid? fromUserId, string? fromUserType,
        Guid toUserId, string toUserType,
        decimal amount, string idempotencyKey, string? description,
        Guid actorUserId, BackofficeUserRole actorRole, Guid brandId,
        CancellationToken ct = default);

    /// <summary>
    /// Obtener transacciones con filtros y scope por rol/brand
    /// </summary>
    Task<GetTransactionsResponse> GetTransactionsAsync(GetTransactionsRequest request, Guid? brandScope, Guid actorUserId, BackofficeUserRole actorRole);

    /// <summary>
    /// Obtener balance de usuario (BACKOFFICE o PLAYER)
    /// </summary>
    Task<SimpleWalletBalanceResponse?> GetBalanceAsync(Guid userId, string userType);
}