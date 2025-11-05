using Casino.Application.Abstractions;
using Casino.Application.Features.Auth;
using Casino.Application.Features.Auth.Commands;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Auth;

public class LoginHandler(AppDbContext db, IJwtService jwt) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) throw new UnauthorizedAccessException();

        var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!ok) throw new UnauthorizedAccessException();

        var token = jwt.CreateToken(user.Id, user.Email, user.Role.ToString());
        return new AuthResponse(user.Id, user.Email, user.Role.ToString(), token);
    }
}
