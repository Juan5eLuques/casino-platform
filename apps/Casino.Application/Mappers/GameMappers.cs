using Casino.Application.DTOs.Game;
using Casino.Application.Services.Models;
using Casino.Domain.Entities;

namespace Casino.Application.Mappers;

public static class GameMappers
{
    public static CatalogGameResponse ToCatalogDto(this GetBrandGameResult result)
    {
        // Note: GetBrandGameResult doesn't have extended fields yet
        // We'll need to update it or fetch from Game entity
        return new CatalogGameResponse(
            result.GameId,
            result.Code,
            result.Name,
            result.Provider,
            result.Type.ToString(),// ? Convert enum to string
            result.Category ?? "other",
            result.ImageUrl,
            result.RTP,
            result.Volatility,
            result.MinBet,
            result.MaxBet,
            result.IsFeatured,
            result.IsNew,
            result.Enabled,
            result.DisplayOrder,
            result.Tags);
    }

    public static GetBrandGameResponse ToDto(this BrandGame brandGame)
    {
        return new GetBrandGameResponse(
            brandGame.BrandId,
            brandGame.GameId,
            brandGame.Game.Code,
            brandGame.Game.Name,
            brandGame.Game.Provider,
            brandGame.Enabled,
            brandGame.DisplayOrder,
            brandGame.Tags);
    }

    public static GetGameResponse ToDto(this Game game)
    {
        return new GetGameResponse(
            game.Id,
            game.Code,
            game.Provider,
            game.Name,
            game.LaunchId,
            game.Type,
            game.RTP,
            game.Volatility,
            game.Category,
            game.ImageUrl,
            game.MinBet,
            game.MaxBet,
            game.IsFeatured,
            game.IsNew,
            game.AdditionalTags,
            game.Enabled,
            game.CreatedAt,
            game.UpdatedAt);
    }

    public static CreateGameResponse ToCreateDto(this Game game)
    {
        return new CreateGameResponse(
            game.Id,
            game.Code,
            game.Provider,
            game.Name,
            game.Enabled,
            game.CreatedAt);
    }

    public static IEnumerable<CatalogGameResponse> ToCatalogDto(this IEnumerable<GetBrandGameResult> results)
    {
        return results.Select(r => r.ToCatalogDto());
    }

    public static IEnumerable<GetBrandGameResponse> ToDto(this IEnumerable<GetBrandGameResult> results)
    {
        return results.Select(r => new GetBrandGameResponse(
            Guid.Empty, // BrandId will be set by the calling context
            r.GameId,
            r.Code,
            r.Name,
            r.Provider,
            r.Enabled,
            r.DisplayOrder,
            r.Tags));
    }
}