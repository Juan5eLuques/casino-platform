using Casino.Application.DTOs.UnifiedUser;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// Servicio unificado que combina BackofficeUsers y Players en una sola vista
/// Respeta el scope y jerarquía de permisos del usuario actual
/// </summary>
public class UnifiedUserService : IUnifiedUserService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<UnifiedUserService> _logger;

    public UnifiedUserService(CasinoDbContext context, ILogger<UnifiedUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QueryUnifiedUsersResponse> GetAllUsersAsync(
        QueryUnifiedUsersRequest request, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        _logger.LogInformation("Getting unified users - Role: {Role}, BrandScope: {BrandScope}, UserType: {UserType}", 
            currentRole, brandScope, request.UserType);

        var backofficeUsers = new List<UnifiedUserResponse>();
        var players = new List<UnifiedUserResponse>();

        // Obtener usuarios backoffice si no se filtra solo por players
        if (request.UserType != "PLAYER")
        {
            backofficeUsers = await GetBackofficeUsersAsync(request, brandScope, currentUserId, currentRole);
        }

        // Obtener players si no se filtra solo por backoffice
        if (request.UserType != "BACKOFFICE")
        {
            players = await GetPlayersAsync(request, brandScope, currentUserId, currentRole);
        }

        // Combinar y ordenar resultados
        var allUsers = backofficeUsers.Concat(players)
            .OrderByDescending(u => u.CreatedAt)
            .ToList();

        // Aplicar filtros adicionales
        if (!string.IsNullOrEmpty(request.Username))
        {
            allUsers = allUsers.Where(u => u.Username.Contains(request.Username, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            allUsers = allUsers.Where(u => u.Status == request.Status).ToList();
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            allUsers = allUsers.Where(u => u.Role == request.Role).ToList();
        }

        if (request.CreatedFrom.HasValue)
        {
            allUsers = allUsers.Where(u => u.CreatedAt >= request.CreatedFrom.Value).ToList();
        }

        if (request.CreatedTo.HasValue)
        {
            allUsers = allUsers.Where(u => u.CreatedAt <= request.CreatedTo.Value).ToList();
        }

        // Paginación
        var totalCount = allUsers.Count;
        var paginatedUsers = allUsers
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var appliedScope = request.GlobalScope && currentRole == BackofficeUserRole.SUPER_ADMIN 
            ? "global" 
            : $"brand:{brandScope}";

        _logger.LogInformation("Unified users query completed - Total: {Total}, Returned: {Returned}, Scope: {Scope}", 
            totalCount, paginatedUsers.Count, appliedScope);

        return new QueryUnifiedUsersResponse(
            paginatedUsers,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize),
            appliedScope
        );
    }

    public async Task<UnifiedUserResponse?> GetUserByIdAsync(
        Guid userId, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        // Buscar en BackofficeUsers primero
        var backofficeUser = await GetBackofficeUserByIdAsync(userId, brandScope, currentUserId, currentRole);
        if (backofficeUser != null)
        {
            return backofficeUser;
        }

        // Si no se encuentra, buscar en Players
        var player = await GetPlayerByIdAsync(userId, brandScope, currentUserId, currentRole);
        return player;
    }

    public async Task<UnifiedUserResponse?> FindUserByUsernameAsync(
        string username, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        // Buscar en BackofficeUsers primero
        var backofficeUser = await FindBackofficeUserByUsernameAsync(username, brandScope, currentUserId, currentRole);
        if (backofficeUser != null)
        {
            return backofficeUser;
        }

        // Si no se encuentra, buscar en Players
        var player = await FindPlayerByUsernameAsync(username, brandScope, currentUserId, currentRole);
        return player;
    }

    // Métodos privados para obtener usuarios backoffice
    private async Task<List<UnifiedUserResponse>> GetBackofficeUsersAsync(
        QueryUnifiedUsersRequest request, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Brand)
            .Include(u => u.ParentCashier)
            .Include(u => u.CreatedByUser) // SONNET: Incluir información del creador
            .AsQueryable();

        // Aplicar scope según rol
        if (currentRole == BackofficeUserRole.SUPER_ADMIN && request.GlobalScope)
        {
            // SUPER_ADMIN con global scope: ver todos
        }
        else if (currentRole == BackofficeUserRole.SUPER_ADMIN || currentRole == BackofficeUserRole.BRAND_ADMIN)
        {
            // SUPER_ADMIN sin global scope o BRAND_ADMIN: solo su brand
            if (brandScope.HasValue)
            {
                query = query.Where(u => u.BrandId == brandScope.Value || u.BrandId == null); // Incluir SUPER_ADMINs
            }
        }
        else if (currentRole == BackofficeUserRole.CASHIER)
        {
            // CASHIER: solo usuarios creados por él + él mismo
            query = query.Where(u => u.CreatedByUserId == currentUserId || u.Id == currentUserId);
        }

        var users = await query.ToListAsync();

        return users.Select(u => new UnifiedUserResponse(
            u.Id,
            "BACKOFFICE",
            u.Username,
            null, // Email
            u.Role.ToString(),
            u.Status.ToString(),
            u.BrandId,
            u.Brand?.Name,
            u.ParentCashierId,
            u.ParentCashier?.Username,
            u.CommissionPercent, // SONNET: Actualizado
            0, // SubordinatesCount - calcular si es necesario
            u.WalletBalance,
            u.CreatedAt,
            u.LastLoginAt,
            // SONNET: Información del creador
            u.CreatedByUserId,
            u.CreatedByUser?.Username,
            u.CreatedByRole
        )).ToList();
    }

    // Métodos privados para obtener players
    private async Task<List<UnifiedUserResponse>> GetPlayersAsync(
        QueryUnifiedUsersRequest request, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.CreatedByUser) // SONNET: Incluir información del creador
            .AsQueryable();

        // Aplicar scope según rol
        if (currentRole == BackofficeUserRole.SUPER_ADMIN && request.GlobalScope)
        {
            // SUPER_ADMIN con global scope: ver todos
        }
        else if (currentRole == BackofficeUserRole.SUPER_ADMIN || currentRole == BackofficeUserRole.BRAND_ADMIN)
        {
            // SUPER_ADMIN sin global scope o BRAND_ADMIN: solo su brand
            if (brandScope.HasValue)
            {
                query = query.Where(p => p.BrandId == brandScope.Value);
            }
        }
        else if (currentRole == BackofficeUserRole.CASHIER)
        {
            // CASHIER: solo players asignados a él
            query = query.Where(p => p.CreatedByUserId == currentUserId); // SONNET: Corregido - usar CreatedByUserId
        }

        var players = await query.ToListAsync();

        return players.Select(p => new UnifiedUserResponse(
            p.Id,
            "PLAYER",
            p.Username,
            p.Email,
            null, // Role
            p.Status.ToString(),
            p.BrandId,
            p.Brand?.Name,
            null, // ParentCashierId
            null, // ParentCashierUsername
            0, // CommissionPercent
            0, // SubordinatesCount
            p.WalletBalance,
            p.CreatedAt,
            null, // LastLoginAt
            // SONNET: Información del creador
            p.CreatedByUserId,
            p.CreatedByUser?.Username,
            p.CreatedByRole
        )).ToList();
    }

    private async Task<UnifiedUserResponse?> GetBackofficeUserByIdAsync(
        Guid userId, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Brand)
            .Include(u => u.ParentCashier)
            .Include(u => u.CreatedByUser) // SONNET: Incluir creador
            .Where(u => u.Id == userId);

        // Aplicar filtros de scope si necesario
        if (currentRole == BackofficeUserRole.CASHIER)
        {
            query = query.Where(u => u.CreatedByUserId == currentUserId || u.Id == currentUserId);
        }
        else if (currentRole != BackofficeUserRole.SUPER_ADMIN && brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value || u.BrandId == null);
        }

        var user = await query.FirstOrDefaultAsync();
        if (user == null) return null;

        return new UnifiedUserResponse(
            user.Id,
            "BACKOFFICE",
            user.Username,
            null, // Email
            user.Role.ToString(),
            user.Status.ToString(),
            user.BrandId,
            user.Brand?.Name,
            user.ParentCashierId,
            user.ParentCashier?.Username,
            user.CommissionPercent,
            0, // SubordinatesCount
            user.WalletBalance,
            user.CreatedAt,
            user.LastLoginAt,
            user.CreatedByUserId,
            user.CreatedByUser?.Username,
            user.CreatedByRole
        );
    }

    private async Task<UnifiedUserResponse?> GetPlayerByIdAsync(
        Guid userId, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.CreatedByUser) // SONNET: Incluir creador
            .Where(p => p.Id == userId);

        // Aplicar filtros de scope si necesario
        if (currentRole == BackofficeUserRole.CASHIER)
        {
            query = query.Where(p => p.CreatedByUserId == currentUserId);
        }
        else if (currentRole != BackofficeUserRole.SUPER_ADMIN && brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync();
        if (player == null) return null;

        return new UnifiedUserResponse(
            player.Id,
            "PLAYER",
            player.Username,
            player.Email,
            null, // Role
            player.Status.ToString(),
            player.BrandId,
            player.Brand?.Name,
            null, // ParentCashierId
            null, // ParentCashierUsername
            0, // CommissionPercent
            0, // SubordinatesCount
            player.WalletBalance,
            player.CreatedAt,
            null, // LastLoginAt
            player.CreatedByUserId,
            player.CreatedByUser?.Username,
            player.CreatedByRole
        );
    }

    private async Task<UnifiedUserResponse?> FindBackofficeUserByUsernameAsync(
        string username, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Brand)
            .Include(u => u.ParentCashier)
            .Include(u => u.CreatedByUser) // SONNET: Incluir creador
            .Where(u => u.Username == username);

        // Aplicar filtros de scope si necesario
        if (currentRole == BackofficeUserRole.CASHIER)
        {
            query = query.Where(u => u.CreatedByUserId == currentUserId || u.Id == currentUserId);
        }
        else if (currentRole != BackofficeUserRole.SUPER_ADMIN && brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value || u.BrandId == null);
        }

        var user = await query.FirstOrDefaultAsync();
        if (user == null) return null;

        return new UnifiedUserResponse(
            user.Id,
            "BACKOFFICE",
            user.Username,
            null, // Email
            user.Role.ToString(),
            user.Status.ToString(),
            user.BrandId,
            user.Brand?.Name,
            user.ParentCashierId,
            user.ParentCashier?.Username,
            user.CommissionPercent,
            0, // SubordinatesCount
            user.WalletBalance,
            user.CreatedAt,
            user.LastLoginAt,
            user.CreatedByUserId,
            user.CreatedByUser?.Username,
            user.CreatedByRole
        );
    }

    private async Task<UnifiedUserResponse?> FindPlayerByUsernameAsync(
        string username, 
        Guid? brandScope, 
        Guid currentUserId, 
        BackofficeUserRole currentRole)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.CreatedByUser) // SONNET: Incluir creador
            .Where(p => p.Username == username);

        // Aplicar filtros de scope si necesario
        if (currentRole == BackofficeUserRole.CASHIER)
        {
            query = query.Where(p => p.CreatedByUserId == currentUserId); // SONNET: Corregido
        }
        else if (currentRole != BackofficeUserRole.SUPER_ADMIN && brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync();
        if (player == null) return null;

        return new UnifiedUserResponse(
            player.Id,
            "PLAYER",
            player.Username,
            player.Email,
            null, // Role
            player.Status.ToString(),
            player.BrandId,
            player.Brand?.Name,
            null, // ParentCashierId
            null, // ParentCashierUsername
            0, // CommissionPercent
            0, // SubordinatesCount
            player.WalletBalance,
            player.CreatedAt,
            null, // LastLoginAt
            player.CreatedByUserId,
            player.CreatedByUser?.Username,
            player.CreatedByRole
        );
    }
}