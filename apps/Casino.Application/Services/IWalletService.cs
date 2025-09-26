using Casino.Application.DTOs.Wallet;

namespace Casino.Application.Services;

public interface IWalletService
{
    Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);
    Task<WalletOperationResponse> DebitAsync(DebitRequest request);
    Task<WalletOperationResponse> CreditAsync(CreditRequest request);
    Task<WalletOperationResponse> RollbackAsync(RollbackRequest request);
}