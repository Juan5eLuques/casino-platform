using Casino.Application.DTOs.Wallet;

namespace Casino.Application.Services;

/// <summary>
/// SONNET: Servicio de wallet legado SOLO para compatibilidad con gateway
/// Mantiene el sistema bigint existente para providers externos
/// </summary>
public interface ILegacyWalletService
{
    Task<WalletBalanceResponse> GetBalanceAsync(WalletBalanceRequest request);
    Task<WalletOperationResponse> DebitAsync(WalletDebitRequest request);
    Task<WalletOperationResponse> CreditAsync(WalletCreditRequest request);
    Task<WalletOperationResponse> RollbackAsync(WalletRollbackRequest request);
}