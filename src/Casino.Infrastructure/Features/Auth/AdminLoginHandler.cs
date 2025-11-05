using Casino.Application.Features.Auth;
using Casino.Application.Features.Auth.Commands;
using Casino.Application.Abstractions;
using Casino.Domain.Users;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Auth;

public class AdminLoginHandler : IRequestHandler<AdminLoginCommand, AuthResponse>
{
    private readonly AppDbContext db;
    private readonly IJwtService jwt;

    public AdminLoginHandler(AppDbContext db, IJwtService jwt)
    {
        this.db = db;
        this.jwt = jwt;
    }

    public async Task<AuthResponse> Handle(AdminLoginCommand req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) throw new UnauthorizedAccessException();

        // Verificar rol: sólo ADMIN o CASHIER (y SUPERADMIN)
        if (user.Role != Role.ADMIN && user.Role != Role.CASHIER && user.Role != Role.SUPERADMIN)
            throw new UnauthorizedAccessException("Acceso denegado: sólo ADMIN o CASHIER.");

        var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!ok) throw new UnauthorizedAccessException();

        var token = jwt.CreateToken(user.Id, user.Email, user.Role.ToString());
        return new AuthResponse(user.Id, user.Email, user.Role.ToString(), token);
    }
}
