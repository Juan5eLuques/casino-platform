using System.Linq;                 // por Select/ToArray
using System.Security.Claims;
using Casino.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace Casino.Infrastructure.Identity;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public int? Id
    {
        get
        {
            var val = User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("id")?.Value;
            return int.TryParse(val, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public string[] Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();
}
