using Casino.Application.DTOs.Wallet;
using Casino.Domain.Enums;

namespace Casino.Application.Services;

/// <summary>
/// Servicio de administración de transacciones que usa UnifiedWalletService internamente
/// Proporciona endpoints administrativos con TransactionType completo
/// Unifica gateway y backoffice en el mismo sistema de WalletTransactions
/// </summary>
public interface IAdminTransactionService
{
    /// <summary>
    /// Crear transacción administrativa (MINT, TRANSFER, DEPOSIT, WITHDRAWAL, etc.)
    /// </summary>
    Task<AdminTransactionResponse> CreateTransactionAsync(
        CreateAdminTransactionRequest request, 
        Guid actorUserId, 
        BackofficeUserRole actorRole, 
        Guid brandId);

    /// <summary>
    /// Obtener transacciones con filtros y scope
    /// </summary>
    Task<GetAdminTransactionsResponse> GetTransactionsAsync(
        GetAdminTransactionsRequest request, 
        Guid? brandScope, 
        Guid actorUserId, 
        BackofficeUserRole actorRole);

    /// <summary>
    /// Revertir transacción por ExternalRef
    /// </summary>
    Task<AdminTransactionResponse> RollbackTransactionAsync(
        AdminRollbackRequest request, 
        Guid actorUserId, 
        BackofficeUserRole actorRole, 
        Guid brandId);

    /// <summary>
    /// Obtener balance de jugador
    /// </summary>
    Task<decimal> GetPlayerBalanceAsync(Guid playerId);

    /// <summary>
    /// Obtener balance de usuario (PLAYER o BACKOFFICE)
    /// </summary>
    Task<object?> GetUserBalanceAsync(Guid userId, string userType);
}