using Casino.Application.Abstractions;
using Casino.Application.Features.Auth;
using Casino.Application.Features.Auth.Commands;
using Casino.Domain.Users;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Auth;

public class RegisterHandler(AppDbContext db, IJwtService jwt) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email ya registrado.");

        // Para el registro público, necesitamos también un username
        // Si no se proporciona, usar la parte antes del @ del email
        var username = !string.IsNullOrWhiteSpace(req.Username) 
            ? req.Username.Trim() 
            : email.Split('@')[0];

        if (await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower(), ct))
            throw new InvalidOperationException("Username ya registrado.");

        // El registro público solo permite crear jugadores
        var role = Role.PLAYER;
        // Solo permitir especificar rol si es SUPERADMIN para casos especiales
        if (!string.IsNullOrWhiteSpace(req.Role) && req.Role.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase))
        {
            // Permitir crear SUPERADMIN solo si no existe ninguno
            var existingSuperAdmin = await db.Users.AnyAsync(u => u.Role == Role.SUPERADMIN, ct);
            if (!existingSuperAdmin)
            {
                role = Role.SUPERADMIN;
            }
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = jwt.CreateToken(user.Id, user.Email, user.Role.ToString());
        return new AuthResponse(user.Id, user.Email, user.Role.ToString(), token);
    }
}
