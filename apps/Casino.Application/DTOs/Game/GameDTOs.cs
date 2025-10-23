namespace Casino.Application.DTOs.Game;

// ? EXTENDED REQUEST DTOs

/// <summary>
/// Request extendido para crear juego con todos los campos del catálogo
/// </summary>
public record CreateGameRequest(
    string Code,
    string Provider,
    string Name,
    string? LaunchId = null,
    Casino.Domain.Enums.GameType Type = Casino.Domain.Enums.GameType.SLOT,
    decimal? RTP = null,
    string? Volatility = null,
    string? Category = null,
    string? ImageUrl = null,
   decimal? MinBet = null,
    decimal? MaxBet = null,
 bool IsFeatured = false,
    bool IsNew = false,
    string[]? AdditionalTags = null,
    bool Enabled = true
);

public record CreateGameResponse(
    Guid Id,
    string Code,
    string Provider,
    string Name,
    bool Enabled,
    DateTime CreatedAt
);

// ? EXTENDED GET RESPONSE with all catalog fields
public record GetGameResponse(
    Guid Id,
 string Code,
    string Provider,
    string Name,
    string? LaunchId,
    Casino.Domain.Enums.GameType Type,
    decimal? RTP,
    string? Volatility,
    string? Category,
    string? ImageUrl,
  decimal? MinBet,
    decimal? MaxBet,
    bool IsFeatured,
    bool IsNew,
    string[] AdditionalTags,
    bool Enabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpdateGameRequest(
    string? Name = null,
    string? LaunchId = null,
    Casino.Domain.Enums.GameType? Type = null,
    decimal? RTP = null,
    string? Volatility = null,
    string? Category = null,
    string? ImageUrl = null,
    decimal? MinBet = null,
    decimal? MaxBet = null,
    bool? IsFeatured = null,
    bool? IsNew = null,
string[]? AdditionalTags = null,
    bool? Enabled = null
);

public record AssignGameToBrandRequest(
    Guid BrandId,
    Guid GameId,
bool Enabled = true,
    int DisplayOrder = 0,
 string[]? Tags = null
);

// DTO para admin que incluye BrandId
public record GetBrandGameResponse(
    Guid BrandId,
    Guid GameId,
    string GameCode,
    string GameName,
    string Provider,
    bool Enabled,
    int DisplayOrder,
    string[] Tags
);

// ? EXTENDED DTO para catálogo público con todos los campos
public record CatalogGameResponse(
    Guid GameId,
    string Code,
    string Name,
    string Provider,
    string Type,
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
    string[] Tags
);

// ? Response con paginación
public record CatalogGamesResponse(
    IEnumerable<CatalogGameResponse> Games,
 int Page,
int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record UpdateBrandGameRequest(
    Guid BrandId,
    Guid GameId,
    bool? Enabled = null,
    int? DisplayOrder = null,
    string[]? Tags = null
);