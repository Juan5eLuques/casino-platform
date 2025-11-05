using MediatR;
using System.Collections.Generic;

namespace Casino.Application.Features.Transactions;

public record TransferHistoryQuery : IRequest<List<TransferHistoryItem>>;
public record TransferHistoryItem(int Id, int FromUserId, int ToUserId, decimal Amount, DateTime Date);
