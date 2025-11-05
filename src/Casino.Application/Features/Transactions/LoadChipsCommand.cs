using MediatR;

namespace Casino.Application.Features.Transactions;

public record LoadChipsCommand(int ToUserId, decimal Amount) : IRequest<TransferResult>;
