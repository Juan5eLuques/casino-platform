using MediatR;

namespace Casino.Application.Features.Transactions;

public record UnloadChipsCommand(int FromUserId, decimal Amount) : IRequest<TransferResult>;
