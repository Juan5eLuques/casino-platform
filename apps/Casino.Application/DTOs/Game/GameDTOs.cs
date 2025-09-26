namespace Casino.Application.DTOs.Game;

public record CreateGameRequest(
    string Code,
    string Provider,
    string Name,
    bool Enabled = true);

public record CreateGameResponse(
    Guid Id,
    string Code,
    string Provider,
    string Name,
    bool Enabled,
    DateTime CreatedAt);

public record GetGameResponse(
    Guid Id,
    string Code,
    string Provider,
    string Name,
    bool Enabled,
    DateTime CreatedAt);

public record UpdateGameRequest(
    string? Name = null,
    bool? Enabled = null);

public record AssignGameToBrandRequest(
    Guid BrandId,
    Guid GameId,
    bool Enabled = true,
    int DisplayOrder = 0,
    string[]? Tags = null);

public record GetBrandGameResponse(
    Guid BrandId,
    Guid GameId,
    string GameCode,
    string GameName,
    string Provider,
    bool Enabled,
    int DisplayOrder,
    string[] Tags);

public record UpdateBrandGameRequest(
    Guid BrandId,
    Guid GameId,
    bool? Enabled = null,
    int? DisplayOrder = null,
    string[]? Tags = null);