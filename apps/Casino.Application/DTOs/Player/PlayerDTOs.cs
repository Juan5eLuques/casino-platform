using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Player;

// Request DTOs - SIN BrandId (se resuelve automáticamente por Host)
public record CreatePlayerRequest(
    string Username,
    string? Email = null,
    string? ExternalId = null,
    long InitialBalance = 0,
    PlayerStatus Status = PlayerStatus.ACTIVE,
    string? Password = null); // Password opcional para jugadores

public record UpdatePlayerRequest(
    string? Username = null,
    string? Email = null,
    PlayerStatus? Status = null);

public record QueryPlayersRequest(
    string? Username = null,
    string? Email = null,
    PlayerStatus? Status = null,
    bool GlobalScope = false, // Solo para SUPER_ADMIN: ver players de todos los brands
    int Page = 1,
    int PageSize = 20);

public record AdjustPlayerWalletRequest(
    long Amount, // Puede ser positivo (crédito) o negativo (débito)
    string Reason,
    string? Description = null);

// Response DTOs
public record GetPlayerResponse(
    Guid Id,
    Guid BrandId,
    string BrandCode,
    string BrandName,
    string Username,
    string? Email,
    string? ExternalId,
    PlayerStatus Status,
    long Balance,
    DateTime CreatedAt);

public record QueryPlayersResponse(
    IEnumerable<GetPlayerResponse> Players,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record WalletAdjustmentResponse(
    bool Success,
    long NewBalance,
    long LedgerId,
    string? ErrorMessage = null);