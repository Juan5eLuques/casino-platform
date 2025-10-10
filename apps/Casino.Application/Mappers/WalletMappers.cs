using Casino.Application.DTOs.Wallet;

namespace Casino.Application.Mappers;

public static class WalletMappers
{
    public static WalletBalanceResponse ToBalanceDto(long balanceBigint)
    {
        return new WalletBalanceResponse(balanceBigint / 100.0m);
    }

    public static WalletOperationResponse ToOperationDto(bool success, decimal? newBalance, Guid? operationId = null, string? message = null)
    {
        return new WalletOperationResponse(success, operationId, message ?? "Operation completed", newBalance);
    }
}