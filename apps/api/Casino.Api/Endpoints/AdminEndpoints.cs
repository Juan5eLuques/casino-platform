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

        // Backoffice Users
        group.MapPost("/users", CreateBackofficeUser)
            .WithName("AdminCreateBackofficeUser");
        group.MapGet("/users", GetBackofficeUsers)
            .WithName("AdminGetBackofficeUsers");
        group.MapPatch("/users/{id:guid}/status", UpdateUserStatus)
            .WithName("AdminUpdateUserStatus");

        // Players Management
        group.MapGet("/players", GetPlayers)
            .WithName("AdminGetPlayers");
        group.MapPatch("/players/{id:guid}/status", UpdatePlayerStatus)
            .WithName("AdminUpdatePlayerStatus");
        group.MapPost("/players/{id:guid}/wallet/adjust", AdjustPlayerWallet)
            .WithName("AdminAdjustPlayerWallet");

        // Cashier Management
        group.MapPost("/cashiers/{cashierId:guid}/assign-player/{playerId:guid}", AssignPlayerToCashier)
            .WithName("AdminAssignPlayerToCashier");
        group.MapGet("/cashiers/{cashierId:guid}/players", GetCashierPlayers)
            .WithName("AdminGetCashierPlayers");
        group.MapDelete("/cashiers/{cashierId:guid}/players/{playerId:guid}", UnassignPlayerFromCashier)
            .WithName("AdminUnassignPlayerFromCashier");

        // Audit endpoints
        group.MapGet("/audit/backoffice", GetBackofficeAudit)
            .WithName("AdminGetBackofficeAudit");
        group.MapGet("/audit/provider", GetProviderAudit)
            .WithName("AdminGetProviderAudit");
    }

    public record CreateBackofficeUserRequest(
        string Username,
        string Password,
        BackofficeUserRole Role,
        Guid? OperatorId = null);

    public record UpdateUserStatusRequest(BackofficeUserStatus Status);
    public record UpdatePlayerStatusRequest(PlayerStatus Status);
    public record AdjustWalletRequest(long Amount, string Reason, string? Notes = null);

    private static async Task<IResult> CreateBackofficeUser(
        [FromBody] CreateBackofficeUserRequest request,
        CasinoDbContext context,
        IAuditService auditService,
        ILogger<Program> logger)
    {
        try
        {
            var existingUser = await context.BackofficeUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (existingUser != null)
            {
                return Results.Problem("Username Already Exists", statusCode: 409);
            }

            var user = new BackofficeUser
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                OperatorId = request.OperatorId,
                Status = BackofficeUserStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };

            context.BackofficeUsers.Add(user);
            await context.SaveChangesAsync();

            // TODO: Get current admin user ID from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            await auditService.LogBackofficeActionAsync(currentUserId, "USER_CREATED", "BackofficeUser", 
                user.Id.ToString(), new { Username = request.Username, Role = request.Role });

            logger.LogInformation("Backoffice user created: {Username}", request.Username);
            return TypedResults.Created($"/api/v1/admin/users/{user.Id}", user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user: {Username}", request.Username);
            return Results.Problem("Internal Server Error", statusCode: 500);
        }
    }

    private static async Task<IResult> GetBackofficeUsers(
        CasinoDbContext context,
        Guid? operatorId = null)
    {
        var query = context.BackofficeUsers.Include(u => u.Operator).AsNoTracking();
        
        if (operatorId.HasValue)
            query = query.Where(u => u.OperatorId == operatorId.Value);

        var users = await query.Select(u => new
        {
            u.Id,
            u.Username,
            u.Role,
            u.Status,
            u.CreatedAt,
            Operator = u.Operator != null ? new { u.Operator.Id, u.Operator.Name } : null
        }).ToListAsync();

        return TypedResults.Ok(users);
    }

    private static async Task<IResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
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
            user.Id.ToString(), new { OldStatus = oldStatus, NewStatus = request.Status });

        return TypedResults.Ok(user);
    }

    private static async Task<IResult> GetPlayers(
        CasinoDbContext context,
        Guid? brandId = null)
    {
        var query = context.Players.Include(p => p.Brand).Include(p => p.Wallet).AsNoTracking();
        
        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        var players = await query.Select(p => new
        {
            p.Id,
            p.Username,
            p.Status,
            Brand = new { p.Brand.Name, p.Brand.Code },
            Balance = p.Wallet != null ? p.Wallet.BalanceBigint : 0
        }).ToListAsync();

        return TypedResults.Ok(players);
    }

    private static async Task<IResult> UpdatePlayerStatus(
        Guid id,
        [FromBody] UpdatePlayerStatusRequest request,
        CasinoDbContext context,
        IAuditService auditService)
    {
        var player = await context.Players.FindAsync(id);
        if (player == null)
            return Results.Problem("Player Not Found", statusCode: 404);

        var oldStatus = player.Status;
        player.Status = request.Status;
        await context.SaveChangesAsync();

        // TODO: Get current admin user ID from JWT/auth context
        var currentUserId = Guid.NewGuid(); // Placeholder
        await auditService.LogBackofficeActionAsync(currentUserId, "PLAYER_STATUS_UPDATED", "Player", 
            player.Id.ToString(), new { OldStatus = oldStatus, NewStatus = request.Status });

        return TypedResults.Ok(player);
    }

    private static async Task<IResult> AdjustPlayerWallet(
        Guid id,
        [FromBody] AdjustWalletRequest request,
        CasinoDbContext context,
        IAuditService auditService)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            var player = await context.Players
                .Include(p => p.Brand)
                .Include(p => p.Wallet)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player?.Wallet == null)
                return Results.Problem("Player or Wallet Not Found", statusCode: 404);

            var oldBalance = player.Wallet.BalanceBigint;
            var newBalance = player.Wallet.BalanceBigint + request.Amount;
            if (newBalance < 0)
                return Results.Problem("Invalid Adjustment", statusCode: 400);

            player.Wallet.BalanceBigint = newBalance;

            var ledgerEntry = new Ledger
            {
                OperatorId = player.Brand.OperatorId,
                BrandId = player.BrandId,
                PlayerId = player.Id,
                DeltaBigint = request.Amount,
                Reason = request.Amount > 0 ? LedgerReason.ADMIN_GRANT : LedgerReason.ADMIN_DEBIT,
                ExternalRef = $"ADMIN_ADJUST_{Guid.NewGuid()}",
                CreatedAt = DateTime.UtcNow
            };

            context.Ledger.Add(ledgerEntry);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // TODO: Get current admin user ID from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            await auditService.LogBackofficeActionAsync(currentUserId, "WALLET_ADJUSTED", "Player", 
                player.Id.ToString(), new { 
                    Amount = request.Amount, 
                    OldBalance = oldBalance, 
                    NewBalance = newBalance,
                    Reason = request.Reason,
                    Notes = request.Notes 
                });

            return TypedResults.Ok(new { Success = true, NewBalance = newBalance });
        }
        catch
        {
            await transaction.RollbackAsync();
            return Results.Problem("Internal Server Error", statusCode: 500);
        }
    }

    private static async Task<IResult> AssignPlayerToCashier(
        Guid cashierId,
        Guid playerId,
        CasinoDbContext context,
        IAuditService auditService)
    {
        var cashier = await context.BackofficeUsers
            .FirstOrDefaultAsync(u => u.Id == cashierId && u.Role == BackofficeUserRole.CASHIER);
        
        if (cashier == null)
            return Results.Problem("Cashier Not Found", statusCode: 404);

        var player = await context.Players.FindAsync(playerId);
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
            $"{cashierId}|{playerId}", new { CashierId = cashierId, PlayerId = playerId });

        return TypedResults.Created($"/api/v1/admin/cashiers/{cashierId}/players", assignment);
    }

    private static async Task<IResult> GetCashierPlayers(
        Guid cashierId,
        CasinoDbContext context)
    {
        var players = await context.CashierPlayers
            .Where(cp => cp.CashierId == cashierId)
            .Include(cp => cp.Player.Brand)
            .Include(cp => cp.Player.Wallet)
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
        CasinoDbContext context,
        IAuditService auditService)
    {
        var assignment = await context.CashierPlayers
            .FirstOrDefaultAsync(cp => cp.CashierId == cashierId && cp.PlayerId == playerId);
        
        if (assignment == null)
            return Results.Problem("Assignment Not Found", statusCode: 404);

        context.CashierPlayers.Remove(assignment);
        await context.SaveChangesAsync();

        // TODO: Get current admin user ID from JWT/auth context
        var currentUserId = Guid.NewGuid(); // Placeholder
        await auditService.LogBackofficeActionAsync(currentUserId, "PLAYER_UNASSIGNED_FROM_CASHIER", "CashierPlayer", 
            $"{cashierId}|{playerId}", new { CashierId = cashierId, PlayerId = playerId });

        return TypedResults.Ok(new { Success = true, Message = "Player unassigned successfully" });
    }

    private static async Task<IResult> GetBackofficeAudit(
        CasinoDbContext context,
        Guid? userId = null,
        string? action = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = context.BackofficeAudits
            .Include(a => a.User)
            .AsNoTracking();

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
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    private static async Task<IResult> GetProviderAudit(
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
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }
}