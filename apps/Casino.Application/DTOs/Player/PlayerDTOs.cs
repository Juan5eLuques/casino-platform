using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Player;

// Request DTOs
public record CreatePlayerRequest(
    Guid BrandId,
    string Username,
    string? Email = null,
    string? ExternalId = null,
    long InitialBalance = 0,
    PlayerStatus Status = PlayerStatus.ACTIVE);

public record UpdatePlayerRequest(
    string? Username = null,
    string? Email = null,
    PlayerStatus? Status = null);

public record QueryPlayersRequest(
    Guid? BrandId = null,
    string? Username = null,
    string? Email = null,
    PlayerStatus? Status = null,
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