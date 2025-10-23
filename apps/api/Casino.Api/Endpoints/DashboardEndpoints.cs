using Casino.Application.DTOs.Dashboard;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/dashboard/overview", GetOverview)
    .WithName("GetDashboardOverview")
          .WithTags("Dashboard")
            .WithSummary("Get consolidated dashboard view (all sections)");
 
        group.MapGet("/dashboard/finances/summary", GetFinancesSummary)
  .WithName("GetFinancesSummary")
  .WithTags("Dashboard")
            .WithSummary("Get financial summary for selected period");
  
        group.MapGet("/dashboard/casino/summary", GetCasinoSummary)
        .WithName("GetCasinoSummary")
          .WithTags("Dashboard")
        .WithSummary("Get casino KPIs summary");
      
   group.MapGet("/dashboard/users/counts", GetUsersCounts)
         .WithName("GetUsersCounts")
            .WithTags("Dashboard")
    .WithSummary("Get users counts and breakdown");
        
      group.MapGet("/dashboard/alerts", GetAlerts)
      .WithName("GetDashboardAlerts")
        .WithTags("Dashboard")
            .WithSummary("Get operational alerts and status");
    }
    
    private static async Task<IResult> GetOverview(
        HttpContext httpContext,
        IDashboardService dashboardService,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string timezone = "UTC",
      [FromQuery] Guid? brandId = null,
        [FromQuery] string scope = "TREE")
    {
    var (currentUserId, currentRole, tokenBrandId) = ExtractUserInfo(httpContext);
     if (currentUserId == Guid.Empty) return Results.Unauthorized();
        
        var query = BuildQuery(from, to, timezone, brandId, scope, tokenBrandId, currentRole);
if (query == null) return Results.Forbid();
        
        var result = await dashboardService.GetOverviewAsync(query, currentUserId, currentRole);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetFinancesSummary(
        HttpContext httpContext,
        IDashboardService dashboardService,
      [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
      [FromQuery] string timezone = "UTC",
      [FromQuery] Guid? brandId = null,
    [FromQuery] string scope = "TREE")
    {
   var (currentUserId, currentRole, tokenBrandId) = ExtractUserInfo(httpContext);
      if (currentUserId == Guid.Empty) return Results.Unauthorized();
        
   var query = BuildQuery(from, to, timezone, brandId, scope, tokenBrandId, currentRole);
        if (query == null) return Results.Forbid();
      
        var result = await dashboardService.GetFinancesSummaryAsync(query, currentUserId, currentRole);
     return Results.Ok(result);
    }
    
    private static async Task<IResult> GetCasinoSummary(
HttpContext httpContext,
        IDashboardService dashboardService,
  [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string timezone = "UTC",
        [FromQuery] Guid? brandId = null,
        [FromQuery] string scope = "TREE")
    {
        var (currentUserId, currentRole, tokenBrandId) = ExtractUserInfo(httpContext);
        if (currentUserId == Guid.Empty) return Results.Unauthorized();
        
        var query = BuildQuery(from, to, timezone, brandId, scope, tokenBrandId, currentRole);
        if (query == null) return Results.Forbid();
        
 var result = await dashboardService.GetCasinoSummaryAsync(query, currentUserId, currentRole);
     return Results.Ok(result);
    }
    
    private static async Task<IResult> GetUsersCounts(
        HttpContext httpContext,
  IDashboardService dashboardService,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string timezone = "UTC",
   [FromQuery] Guid? brandId = null,
  [FromQuery] string scope = "TREE")
    {
        var (currentUserId, currentRole, tokenBrandId) = ExtractUserInfo(httpContext);
        if (currentUserId == Guid.Empty) return Results.Unauthorized();
        
        var query = BuildQuery(from, to, timezone, brandId, scope, tokenBrandId, currentRole);
        if (query == null) return Results.Forbid();

        var result = await dashboardService.GetUsersCountsAsync(query, currentUserId, currentRole);
  return Results.Ok(result);
    }
    
    private static async Task<IResult> GetAlerts(
 HttpContext httpContext,
        IDashboardService dashboardService,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string timezone = "UTC",
        [FromQuery] Guid? brandId = null,
        [FromQuery] string scope = "TREE")
    {
        var (currentUserId, currentRole, tokenBrandId) = ExtractUserInfo(httpContext);
        if (currentUserId == Guid.Empty) return Results.Unauthorized();
        
        var query = BuildQuery(from, to, timezone, brandId, scope, tokenBrandId, currentRole);
        if (query == null) return Results.Forbid();
    
 var result = await dashboardService.GetAlertsAsync(query, currentUserId, currentRole);
        return Results.Ok(result);
    }
    
    // === HELPER METHODS ===
    
 private static (Guid userId, string role, Guid brandId) ExtractUserInfo(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var brandIdClaim = httpContext.User.FindFirst("brand_id")?.Value;
    
        var userId = Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty;
        var brandId = Guid.TryParse(brandIdClaim, out var bid) ? bid : Guid.Empty;
      
        return (userId, role, brandId);
    }
    
    private static DashboardQuery? BuildQuery(
 DateTime? from,
        DateTime? to,
string timezone,
        Guid? brandId,
        string scope,
 Guid tokenBrandId,
        string currentRole)
    {
        // Determinar brandId efectivo
        var effectiveBrandId = brandId ?? tokenBrandId;
  
    // Validar permisos de brand
        if (currentRole != "SUPER_ADMIN" && effectiveBrandId != tokenBrandId)
 {
            return null; // Forbid
        }
  
        // Parsear scope
        if (!Enum.TryParse<DashboardScope>(scope, true, out var scopeEnum))
        {
  scopeEnum = DashboardScope.TREE;
        }
        
        // CASHIER no puede usar GLOBAL
    if (currentRole == "CASHIER" && scopeEnum == DashboardScope.GLOBAL)
        {
   return null; // Forbid
        }
        
        return new DashboardQuery
        {
        From = from,
  To = to,
            Timezone = timezone,
    BrandId = effectiveBrandId,
            Scope = scopeEnum
        };
    }
}
