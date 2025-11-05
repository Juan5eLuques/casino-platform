using Casino.Application.Abstractions;
using Casino.Application.Features.Transactions;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Transactions;

public class TransferHistoryHandler : IRequestHandler<TransferHistoryQuery, List<TransferHistoryItem>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TransferHistoryHandler(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<TransferHistoryItem>> Handle(TransferHistoryQuery req, CancellationToken ct)
    {
        var userId = _currentUser.Id ?? 0;
        return await _db.Transfers
            .Where(t => t.FromUserId == userId || t.ToUserId == userId)
            .OrderByDescending(t => t.Date)
            .Select(t => new TransferHistoryItem(t.Id, t.FromUserId, t.ToUserId, t.Amount, t.Date))
            .ToListAsync(ct);
    }
}
