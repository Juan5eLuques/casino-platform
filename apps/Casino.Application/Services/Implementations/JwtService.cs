using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Casino.Application.DTOs.Auth;

namespace Casino.Application.Services;

public interface IJwtService
{
    TokenResponse IssueToken(string audience, IEnumerable<Claim> claims, TimeSpan ttl);
}

public class JwtService : IJwtService
{
    private readonly string _issuer;
    private readonly SymmetricSecurityKey _key;

    public JwtService(IConfiguration configuration)
    {
        _issuer = configuration["Auth:Issuer"] ?? "casino";
        var jwtKey = configuration["Auth:JwtKey"] ?? throw new InvalidOperationException("Auth:JwtKey is required");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    }

    public TokenResponse IssueToken(string audience, IEnumerable<Claim> claims, TimeSpan ttl)
    {
        var now = DateTimeOffset.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: _issuer,
            audience: audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.Add(ttl).UtcDateTime,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        );
        
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new TokenResponse(token, now.Add(ttl).UtcDateTime);
    }
}