namespace Casino.Application.Features.Transactions;

public record TransferResult(bool Success, string? Error = null);
