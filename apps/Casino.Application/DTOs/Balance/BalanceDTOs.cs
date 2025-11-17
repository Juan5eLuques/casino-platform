namespace Casino.Application.DTOs.Balance;

/// <summary>
/// Response para el balance del usuario logueado
/// </summary>
public record UserBalanceResponse(
    Guid UserId,
    string UserType, // "BACKOFFICE" o "PLAYER"
 string Username,
    decimal Balance,
    string? Role, // Solo para BACKOFFICE
    Guid? BrandId,
    string? BrandName
);
