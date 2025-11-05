using Casino.Application.Abstractions;
using Casino.Infrastructure.Persistence;

namespace Casino.Infrastructure;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
