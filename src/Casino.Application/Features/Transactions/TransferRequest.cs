namespace Casino.Application.Features.Transactions;

public record TransferRequest(int ToUserId, decimal Amount);
