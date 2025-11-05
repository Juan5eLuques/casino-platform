using Casino.Application.Abstractions;
using Casino.Application.Features.Auth;
using Casino.Application.Features.Auth.Queries;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Auth;

public class MeHandler(AppDbContext db, ICurrentUser current) : IRequestHandler<MeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(MeQuery req, CancellationToken ct)
    {
        if (current.Id is null) throw new UnauthorizedAccessException();

        var u = await db.Users
            .Where(x => x.Id == current.Id)
            .Select(x => new MeResponse(x.Id, x.Email, x.Role.ToString(), x.Balance, x.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return u ?? throw new UnauthorizedAccessException();
    }
}
