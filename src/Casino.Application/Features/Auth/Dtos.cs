namespace Casino.Application.Features.Auth;

public record RegisterRequest(string Email, string Password, string? Role = null);
public record LoginRequest(string Email, string Password);
public record AuthResponse(int Id, string Email, string Role, string Token);
public record MeResponse(int Id, string Email, string Role, decimal Balance, DateTime CreatedAt);
