namespace Casino.Application.Services.Models;

// Modelos internos para servicios - no expuestos en API
// ? EXTENDED with all catalog fields
public record GetBrandGameResult(
    Guid GameId,
    string Code,
    string Name,
    string Provider,
    Casino.Domain.Enums.GameType Type,
    string? Category,
    string? ImageUrl,
    decimal? RTP,
    string? Volatility,
    decimal? MinBet,
    decimal? MaxBet,
    bool IsFeatured,
    bool IsNew,
    bool Enabled,
    int DisplayOrder,
    string[] Tags);

public record BrandOperationResult(
    bool Success,
    string? ErrorMessage = null,
    object? Data = null);

public record GameOperationResult(
    bool Success,
    string? ErrorMessage = null,
    Guid? GameId = null);

public record SessionOperationResult(
    bool Success,
    Guid? SessionId = null,
    string? ErrorMessage = null);

public record WalletOperationResult(
    bool Success,
    long Balance,
    string? ErrorMessage = null,
    long? LedgerId = null);