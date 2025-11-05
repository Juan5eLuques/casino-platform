using Casino.Application.Abstractions;
using Casino.Application.Features.Users;
using Casino.Application.Features.Users.Queries;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Users;

public class GetMyUsersHandler(AppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetMyUsersQuery, List<UserResponse>>
{
    public async Task<List<UserResponse>> Handle(GetMyUsersQuery req, CancellationToken ct)
    {
        if (currentUser.Id is null)
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var myUsers = await db.Users
            .Include(u => u.ParentUser)
            .Where(u => u.ParentUserId == currentUser.Id)
            .Select(u => new UserResponse(
                u.Id,
                u.Username,
                u.Email,
                u.Role.ToString(),
                u.Balance,
                u.CommissionRate,
                u.ParentUserId,
                u.ParentUser != null ? u.ParentUser.Email : null,
                u.CreatedAt
            ))
            .ToListAsync(ct);

        return myUsers;
    }
}

public class GetUserHierarchyHandler(AppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetUserHierarchyQuery, UserHierarchyResponse>
{
    public async Task<UserHierarchyResponse> Handle(GetUserHierarchyQuery req, CancellationToken ct)
    {
        if (currentUser.Id is null)
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var user = await db.Users
            .Include(u => u.ChildUsers)
                .ThenInclude(c => c.ChildUsers)
                    .ThenInclude(gc => gc.ChildUsers)
            .FirstOrDefaultAsync(u => u.Id == currentUser.Id, ct);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado");

        return MapToHierarchyResponse(user);
    }

    private UserHierarchyResponse MapToHierarchyResponse(Casino.Domain.Users.User user)
    {
        return new UserHierarchyResponse(
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString(),
            user.Balance,
            user.CommissionRate,
            user.ChildUsers.Select(MapToHierarchyResponse).ToList()
        );
    }
}