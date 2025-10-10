using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Cashier;

public record AssignPlayerToCashierRequest();

public record AssignPlayerToCashierResponse(
    Guid CashierId,
    Guid PlayerId,
    string CashierUsername,
    string PlayerUsername,
    DateTime AssignedAt
);

public record GetCashierPlayersResponse(
    Guid CashierId,
    string CashierUsername,
    string Role,
    IEnumerable<CashierPlayerDto> Players
);

public record CashierPlayerDto(
    Guid PlayerId,
    string Username,
    string Email,
    PlayerStatus Status,
    long CurrentBalance,
    DateTime AssignedAt
);

public record GetPlayerCashiersResponse(
    Guid PlayerId,
    string PlayerUsername,
    IEnumerable<PlayerCashierDto> AssignedCashiers
);

public record PlayerCashierDto(
    Guid CashierId,
    string Username,
    BackofficeUserRole Role,
    DateTime AssignedAt
);

public record UnassignPlayerResponse(
    bool Success,
    string Message
);