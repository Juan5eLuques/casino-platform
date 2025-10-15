using Casino.Application.DTOs.Wallet;

namespace Casino.Application.Services;

/// <summary>
/// Servicio unificado de wallet para operaciones de juegos y gateway
/// Usa Player.WalletBalance como source of truth y registra en WalletTransactions
/// Compatible con la interfaz de ILegacyWalletService
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Obtener balance de jugador
    /// </summary>
    Task<WalletBalanceResponse> GetBalanceAsync(WalletBalanceRequest request);

    /// <summary>
    /// Debitar saldo (BET)
    /// </summary>
    Task<WalletOperationResponse> DebitAsync(WalletDebitRequest request);

    /// <summary>
    /// Acreditar saldo (WIN)
    /// </summary>
    Task<WalletOperationResponse> CreditAsync(WalletCreditRequest request);

    /// <summary>
    /// Revertir transacción (ROLLBACK)
    /// </summary>
    Task<WalletOperationResponse> RollbackAsync(WalletRollbackRequest request);
}
