namespace Casino.Application.Services.Models;

// Modelos internos para servicios - no expuestos en API
public record GetBrandGameResult(
    Guid GameId,
    string Code,
    string Name,
    string Provider,
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