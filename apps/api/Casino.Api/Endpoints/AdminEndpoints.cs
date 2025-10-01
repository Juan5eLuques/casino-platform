using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casino.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin");

        // NOTE: Removed duplicate endpoints - now handled by dedicated endpoint classes:
        // - BackofficeUserEndpoints handles /users routes
        // - PlayerManagementEndpoints handles /players routes  
        // - OperatorEndpoints handles /operators routes
        // - BrandGameEndpoints handles /brands/{id}/games routes
        
        // Keeping only unique endpoints not covered by other endpoint classes:

        // User status updates (specific endpoint not covered elsewhere)
        group.MapPatch("/users/{id:guid}/status", UpdateUserStatus)
            .WithName("AdminUpdateUserStatus")
            .WithTags("Admin");

        // Player status updates (specific endpoint not covered elsewhere)  
        group.MapPatch("/players/{id:guid}/status", UpdatePlayerStatus)
            .WithName("AdminUpdatePlayerStatus")
            .WithTags("Admin");

        // Cashier Management (unique functionality)
        group.MapPost("/cashiers/{cashierId:guid}/assign-player/{playerId:guid}", AssignPlayerToCashier)
            .WithName("AdminAssignPlayerToCashier")
            .WithTags("Admin");
        group.MapGet("/cashiers/{cashierId:guid}/players", GetCashierPlayers)
            .WithName("AdminGetCashierPlayers")
            .WithTags("Admin");
        group.MapDelete("/cashiers/{cashierId:guid}/players/{playerId:guid}", UnassignPlayerFromCashier)
            .WithName("AdminUnassignPlayerFromCashier")
            .WithTags("Admin");

        // Audit endpoints (unique functionality)
        group.MapGet("/audit/backoffice", GetBackofficeAudit)
            .WithName("AdminGetBackofficeAudit")
            .WithTags("Admin");
        group.MapGet("/audit/provider", GetProviderAudit)
            .WithName("AdminGetProviderAudit")
            .WithTags("Admin");
    }

    // Keep only DTOs for endpoints that remain
    public record UpdateUserStatusRequest(BackofficeUserStatus Status);
    public record UpdatePlayerStatusRequest(PlayerStatus Status);

    // Remove duplicate DTOs - these are now handled by dedicated DTO classes:
    // - CreateBackofficeUserRequest (now in Casino.Application.DTOs.Admin)
    // - AdjustWalletRequest (now AdjustPlayerWalletRequest in Casino.Application.DTOs.Player)

    private static async Task<IResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        BrandContext brandContext,
        CasinoDbContext context,
        IAuditService auditService,
        ILogger<Program> logger)
    {
        var user = await context.BackofficeUsers.FindAsync(id);
        if (user == null)
            return Results.Problem("User Not Found", statusCode: 404);

        var oldStatus = user.Status;
        user.Status = request.Status;
        await context.SaveChangesAsync();

        // TODO: Get current admin user ID from JWT/auth context
        var currentUserId = Guid.NewGuid(); // Placeholder
        await auditService.LogBackofficeActionAsync(currentUserId, "USER_STATUS_UPDATED", "BackofficeUser", 
            user.Id.ToString(), new { OldStatus = oldStatus, NewStatus = request.Status, BrandCode = brandContext.BrandCode });

        return TypedResults.Ok(user);
    }

    private static async Task<IResult> UpdatePlayerStatus(
        Guid id,
        [FromBody] UpdatePlayerStatusRequest request,
        BrandContext brandContext,
        CasinoDbContext context,
        IAuditService auditService)
    {
        if (!brandContext.IsResolved)
        {
            return Results.Problem(
                title: "Brand Not Resolved",
                detail: "Brand context is not available",
                statusCode: 400);
        }

        // Ensure player belongs to current brand
        var player = await context.Players.FirstOrDefaultAsync(p => 
            p.Id == id && p.BrandId == brandContext.BrandId);
        
        if (player == null)
            return Results.Problem("Player Not Found", statusCode: 404);

        var oldStatus = player.Status;
        player.Status = request.Status;
        await context.SaveChangesAsync();

        // TODO: Get current admin user ID from JWT/auth context
        var currentUserId = Guid.NewGuid(); // Placeholder
        await auditService.LogBackofficeActionAsync(currentUserId, "PLAYER_STATUS_UPDATED", "Player", 
            player.Id.ToString(), new { OldStatus = oldStatus, NewStatus = request.Status, BrandCode = brandContext.BrandCode });

        return TypedResults.Ok(player);
    }

    private static async Task<IResult> AssignPlayerToCashier(
        Guid cashierId,
        Guid playerId,
        BrandContext brandContext,
        CasinoDbContext context,
        IAuditService auditService)
    {
        if (!brandContext.IsResolved)
        {
            return Results.Problem(
                title: "Brand Not Resolved",
                detail: "Brand context is not available",
                statusCode: 400);
        }

        var cashier = await context.BackofficeUsers
            .FirstOrDefaultAsync(u => u.Id == cashierId && 
                                     u.Role == BackofficeUserRole.CASHIER &&
                                     u.OperatorId == brandContext.OperatorId);
        
        if (cashier == null)
            return Results.Problem("Cashier Not Found", statusCode: 404);

        // Ensure player belongs to current brand
        var player = await context.Players.FirstOrDefaultAsync(p => 
            p.Id == playerId && p.BrandId == brandContext.BrandId);
        
        if (player == null)
            return Results.Problem("Player Not Found", statusCode: 404);

        // Check if assignment already exists
        var existingAssignment = await context.CashierPlayers
            .FirstOrDefaultAsync(cp => cp.CashierId == cashierId && cp.PlayerId == playerId);
        
        if (existingAssignment != null)
            return Results.Problem("Player Already Assigned to Cashier", statusCode: 409);

        var assignment = new CashierPlayer
        {
            CashierId = cashierId,
            PlayerId = playerId,
            AssignedAt = DateTime.UtcNow
        };

        context.CashierPlayers.Add(assignment);
        await context.SaveChangesAsync();

        // TODO: Get current admin user ID from JWT/auth context
        var currentUserId = Guid.NewGuid(); // Placeholder
        await auditService.LogBackofficeActionAsync(currentUserId, "PLAYER_ASSIGNED_TO_CASHIER", "CashierPlayer", 
            $"{cashierId}|{playerId}", new { CashierId = cashierId, PlayerId = playerId, BrandCode = brandContext.BrandCode });

        return TypedResults.Created($"/api/v1/admin/cashiers/{cashierId}/players", assignment);
    }

    private static async Task<IResult> GetCashierPlayers(
        Guid cashierId,
        BrandContext brandContext,
        CasinoDbContext context)
    {
        if (!brandContext.IsResolved)
        {
            return Results.Problem(
                title: "Brand Not Resolved",
                detail: "Brand context is not available",
                statusCode: 400);
        }

        // Get players assigned to cashier that belong to current brand
        var players = await context.CashierPlayers
            .Where(cp => cp.CashierId == cashierId)
            .Include(cp => cp.Player.Brand)
            .Include(cp => cp.Player.Wallet)
            .Where(cp => cp.Player.BrandId == brandContext.BrandId)
            .Select(cp => new
            {
                cp.Player.Id,
                cp.Player.Username,
                cp.Player.Status,
                Brand = new { cp.Player.Brand.Name },
                Balance = cp.Player.Wallet != null ? cp.Player.Wallet.BalanceBigint : 0,
                cp.AssignedAt
            })
            .ToListAsync();

        return TypedResults.Ok(players);
    }

    private static async Task<IResult> UnassignPlayerFromCashier(
        Guid cashierId,
        Guid playerId,
        BrandContext brandContext,
        CasinoDbContext context,
        IAuditService auditService)
    {
        if (!brandContext.IsResolved)
        {
            return Results.Problem(
                title: "Brand Not Resolved",
                detail: "Brand context is not available",
                statusCode: 400);
        }

        // Ensure player belongs to current brand
        var assignment = await context.CashierPlayers
            .Include(cp => cp.Player)
            .FirstOrDefaultAsync(cp => cp.CashierId == cashierId && 
                                      cp.PlayerId == playerId &&
                                      cp.Player.BrandId == brandContext.BrandId);
        
        if (assignment == null)
            return Results.Problem("Assignment Not Found", statusCode: 404);

        context.CashierPlayers.Remove(assignment);
        await context.SaveChangesAsync();

        // TODO: Get current admin user ID from JWT/auth context
        var currentUserId = Guid.NewGuid(); // Placeholder
        await auditService.LogBackofficeActionAsync(currentUserId, "PLAYER_UNASSIGNED_FROM_CASHIER", "CashierPlayer", 
            $"{cashierId}|{playerId}", new { CashierId = cashierId, PlayerId = playerId, BrandCode = brandContext.BrandCode });

        return TypedResults.Ok(new { Success = true, Message = "Player unassigned successfully" });
    }

    private static async Task<IResult> GetBackofficeAudit(
        BrandContext brandContext,
        CasinoDbContext context,
        Guid? userId = null,
        string? action = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = context.BackofficeAudits
            .Include(a => a.User)
            .AsNoTracking();

        // Filter by brand's operator if brand context is available
        if (brandContext.IsResolved)
        {
            query = query.Where(a => a.User.OperatorId == brandContext.OperatorId);
        }

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        var totalCount = await query.CountAsync();
        var audits = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.TargetType,
                a.TargetId,
                a.Meta,
                a.CreatedAt,
                User = new { a.User.Username, a.User.Role }
            })
            .ToListAsync();

        return TypedResults.Ok(new
        {
            Data = audits,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            BrandCode = brandContext.BrandCode
        });
    }

    private static async Task<IResult> GetProviderAudit(
        BrandContext brandContext,
        CasinoDbContext context,
        string? provider = null,
        string? action = null,
        string? sessionId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = context.ProviderAudits.AsNoTracking();

        if (!string.IsNullOrEmpty(provider))
            query = query.Where(a => a.Provider == provider);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(a => a.SessionId == sessionId);

        var totalCount = await query.CountAsync();
        var audits = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new
        {
            Data = audits,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            BrandCode = brandContext.BrandCode
        });
    }
}