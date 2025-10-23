using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class CommissionEndpoints
{
    public static void MapCommissionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/commissions/pending", GetPendingCommissions)
            .WithName("GetPendingCommissions")
            .WithTags("Commissions")
            .WithSummary("Get pending commissions for current user or specified user");
        
        group.MapGet("/commissions/breakdown/{playerId}", GetCommissionBreakdown)
            .WithName("GetCommissionBreakdown")
            .WithTags("Commissions")
            .WithSummary("Calculate commission breakdown for a NetWin amount");
        
        group.MapPost("/commissions/settle", SettleCommissions)
            .RequireAuthorization("SuperAdminOnly")
            .WithName("SettleCommissions")
            .WithTags("Commissions")
            .WithSummary("Settle all pending commissions for a period (SUPER_ADMIN only)");
    }
    
    /// <summary>
    /// Obtiene comisiones pendientes de liquidación
    /// </summary>
    private static async Task<IResult> GetPendingCommissions(
        HttpContext httpContext,
        ICommissionService commissionService,
        [FromQuery] Guid? userId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var currentUserIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return Results.Unauthorized();
        }
        
        var currentRole = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Si no se especifica userId, usar el usuario actual
        var targetUserId = userId ?? currentUserId;
        
        // Solo SUPER_ADMIN puede ver comisiones de otros usuarios
        if (targetUserId != currentUserId && currentRole != "SUPER_ADMIN")
        {
            return Results.Forbid();
        }
        
        var period = DateTime.UtcNow;
        var targetYear = year ?? period.Year;
        var targetMonth = month ?? period.Month;
        
        var pendingCommissions = await commissionService.GetPendingCommissionsAsync(
            targetUserId,
            targetYear,
            targetMonth);
        
        var total = await commissionService.GetPendingCommissionsTotalAsync(
            targetUserId,
            targetYear,
            targetMonth);
        
        return Results.Ok(new
        {
            UserId = targetUserId,
            Year = targetYear,
            Month = targetMonth,
            TotalPending = total,
            CommissionCount = pendingCommissions.Count(),
            Commissions = pendingCommissions.Select(c => new
            {
                c.Id,
                c.SourceType,
                c.BaseAmount,
                CommissionRate = c.CommissionRate * 100, // Convert to percentage
                c.CommissionAmount,
                c.CreatedAt,
                SourceTransaction = c.SourceTransactionId,
                SourceRound = c.SourceRoundId,
                SourcePlayer = c.SourcePlayerId != null ? new
                {
                    PlayerId = c.SourcePlayerId,
                    Username = c.SourcePlayer?.Username
                } : null
            })
        });
    }
    
    /// <summary>
    /// Calcula breakdown de comisiones para un NetWin específico
    /// </summary>
    private static async Task<IResult> GetCommissionBreakdown(
        Guid playerId,
        [FromQuery] long netWinAmount,
        ICommissionService commissionService,
        HttpContext httpContext)
    {
        if (netWinAmount <= 0)
        {
            return Results.BadRequest(new { error = "netWinAmount must be positive" });
        }
        
        var breakdown = await commissionService.CalculateCommissionBreakdownAsync(
            playerId,
            netWinAmount);
        
        return Results.Ok(new
        {
            breakdown.PlayerId,
            breakdown.NetWinAmount,
            breakdown.TotalCommissions,
            Levels = breakdown.Levels.Select(l => new
            {
                l.UserId,
                l.Username,
                l.Role,
                l.HierarchyLevel,
                CommissionRate = l.CommissionRate * 100, // Convert to percentage
                l.CommissionAmount,
                l.EffectiveCommission
            })
        });
    }
    
    /// <summary>
    /// Liquida comisiones de un período (solo SUPER_ADMIN)
    /// </summary>
    private static async Task<IResult> SettleCommissions(
        [FromBody] SettleCommissionsRequest request,
        HttpContext httpContext,
        ICommissionService commissionService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("CommissionEndpoints");
        
        var currentUserIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return Results.Unauthorized();
        }
        
        logger.LogInformation(
            "Commission settlement requested by {UserId} for brand {BrandId}, period {Year}-{Month}",
            currentUserId, request.BrandId, request.Year, request.Month);
        
        var result = await commissionService.SettleCommissionsForPeriodAsync(
            request.BrandId,
            request.Year,
            request.Month,
            currentUserId);
        
        if (!result.Success)
        {
            logger.LogError("Commission settlement failed: {Error}", result.ErrorMessage);
            return Results.Problem(
                title: "Settlement Failed",
                detail: result.ErrorMessage,
                statusCode: 500);
        }
        
        logger.LogInformation(
            "Commission settlement completed: {Users} users, Total {Total}",
            result.TotalUsersSettled, result.TotalAmountSettled);
        
        return Results.Ok(new
        {
            success = true,
            result.TotalUsersSettled,
            result.TotalAmountSettled,
            Settlements = result.UserSettlements.Select(s => new
            {
                s.UserId,
                s.Username,
                s.TotalCommission,
                s.TransactionId,
                s.CommissionCount
            })
        });
    }
}

public record SettleCommissionsRequest(
    Guid BrandId,
    int Year,
    int Month
);
