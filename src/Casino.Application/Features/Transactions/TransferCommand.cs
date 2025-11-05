using MediatR;

namespace Casino.Application.Features.Transactions;

public record TransferCommand(int ToUserId, decimal Amount) : IRequest<TransferResult>;
