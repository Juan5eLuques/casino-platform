using Casino.Application.Abstractions;
using Casino.Application.Features.Users;
using Casino.Application.Features.Users.Queries;
using Casino.Domain.Users;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Users;

public class GetAllUsersHandler(AppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetAllUsersQuery, PagedUsersResponse>
{
    public async Task<PagedUsersResponse> Handle(GetAllUsersQuery req, CancellationToken ct)
    {
        if (currentUser.Id is null)
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id, ct);
        if (user == null || (user.Role != Role.SUPERADMIN && user.Role != Role.ADMIN))
            throw new UnauthorizedAccessException("Solo admins y superadmins pueden ver todos los usuarios");

        var query = db.Users.Include(u => u.ParentUser).AsQueryable();

        // Aplicar filtro de búsqueda
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchTerm = req.Search.Trim().ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm)
            );
        }

        // Aplicar filtro de rol
        if (!string.IsNullOrWhiteSpace(req.Role) && Enum.TryParse<Role>(req.Role, true, out var roleEnum))
        {
            query = query.Where(u => u.Role == roleEnum);
        }

        // Aplicar ordenamiento
        query = ApplyOrdering(query, req.OrderBy, req.OrderByDirection);

        // Obtener total de registros
        var totalCount = await query.CountAsync(ct);

        // Calcular paginación
        var totalPages = (int)Math.Ceiling((double)totalCount / req.PerPage);
        var skip = (req.Page - 1) * req.PerPage;

        // Aplicar paginación y obtener resultados
        var users = await query
            .Skip(skip)
            .Take(req.PerPage)
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

        return new PagedUsersResponse(
            users,
            totalCount,
            req.Page,
            req.PerPage,
            totalPages
        );
    }

    private static IQueryable<User> ApplyOrdering(IQueryable<User> query, string orderBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return orderBy.ToLower() switch
        {
            "username" => isDescending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "email" => isDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "role" => isDescending ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
            "balance" => isDescending ? query.OrderByDescending(u => u.Balance) : query.OrderBy(u => u.Balance),
            "createdat" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt) // Default ordering
        };
    }
}