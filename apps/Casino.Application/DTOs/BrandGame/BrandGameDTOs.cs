namespace Casino.Application.DTOs.BrandGame;

// Request DTOs
public record AssignGameToBrandRequest(
    Guid GameId,
    bool Enabled = true,
    int DisplayOrder = 0,
    string[] Tags = null!);

public record UpdateBrandGameRequest(
    bool? Enabled = null,
    int? DisplayOrder = null,
    string[]? Tags = null);

// Response DTOs
public record BrandGameResponse(
    Guid GameId,
    string GameCode,
    string GameName,
    string Provider,
    bool Enabled,
    int DisplayOrder,
    string[] Tags);

public record GetBrandGamesResponse(
    Guid BrandId,
    string BrandCode,
    IEnumerable<BrandGameResponse> Games);