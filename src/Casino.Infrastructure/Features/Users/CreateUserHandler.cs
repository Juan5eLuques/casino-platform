using Casino.Application.Abstractions;
using Casino.Application.Features.Users;
using Casino.Application.Features.Users.Commands;
using Casino.Domain.Users;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Users;

public class CreateUserHandler(AppDbContext db, ICurrentUser currentUser) : IRequestHandler<CreateUserCommand, UserResponse>
{
    public async Task<UserResponse> Handle(CreateUserCommand req, CancellationToken ct)
    {
        if (currentUser.Id is null)
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var creator = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id, ct);
        if (creator == null)
            throw new UnauthorizedAccessException("Usuario no encontrado");

        // Validar permisos según jerarquía
        if (!CanCreateUserWithRole(creator.Role, req.Role))
            throw new UnauthorizedAccessException($"No tiene permisos para crear un usuario con rol {req.Role}");

        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email ya registrado.");

        var username = req.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower(), ct))
            throw new InvalidOperationException("Username ya registrado.");

        if (!Enum.TryParse<Role>(req.Role, true, out var role))
            throw new InvalidOperationException($"Rol '{req.Role}' no válido");

        // Validar comisión solo para cajeros
        var commissionRate = 0m;
        if (role == Role.CASHIER)
        {
            if (req.CommissionRate.HasValue)
            {
                if (req.CommissionRate.Value < 0 || req.CommissionRate.Value > 100)
                    throw new InvalidOperationException("La comisión debe estar entre 0 y 100%");
                commissionRate = req.CommissionRate.Value;
            }
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = role,
            ParentUserId = creator.Id,
            CommissionRate = commissionRate
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString(),
            user.Balance,
            user.CommissionRate,
            user.ParentUserId,
            creator.Email,
            user.CreatedAt
        );
    }

    private static bool CanCreateUserWithRole(Role creatorRole, string targetRole)
    {
        return creatorRole switch
        {
            Role.SUPERADMIN => true, // Puede crear cualquier rol
            Role.ADMIN => targetRole is "ADMIN" or "CASHIER" or "PLAYER",
            Role.CASHIER => targetRole is "CASHIER" or "PLAYER",
            _ => false
        };
    }
}