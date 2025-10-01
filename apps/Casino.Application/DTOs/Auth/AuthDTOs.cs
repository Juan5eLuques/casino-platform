namespace Casino.Application.DTOs.Auth;

public record AdminLoginRequest(
    string Username,
    string Password);

public record PlayerLoginRequest(
    Guid? PlayerId,
    string? Username,
    string Password);

public record TokenResponse(
    string AccessToken,
    DateTime ExpiresAt);

public record LoginResponse(
    bool Success,
    object? User = null,
    DateTime? ExpiresAt = null,
    string? ErrorMessage = null);

public record LogoutResponse(
    bool Success,
    string? Message = null);