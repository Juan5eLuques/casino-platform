using Casino.Application.DTOs.Wallet;
using Casino.Domain.Entities;

namespace Casino.Application.Mappers;

public static class WalletMappers
{
    public static BalanceResponse ToBalanceDto(long balanceBigint)
    {
        return new BalanceResponse(balanceBigint);
    }

    public static WalletOperationResponse ToOperationDto(bool success, long newBalance, long? ledgerId = null, string? errorMessage = null)
    {
        return new WalletOperationResponse(success, newBalance, ledgerId, errorMessage);
    }
}