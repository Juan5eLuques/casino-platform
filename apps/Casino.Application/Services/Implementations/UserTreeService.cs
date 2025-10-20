using Casino.Application.DTOs.Admin;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// Servicio para gestionar el árbol genealógico de usuarios
/// Permite ver qué usuarios fueron creados por otros usuarios de forma jerárquica
/// OPTIMIZADO: Carga todos los datos necesarios en 2 queries (BackofficeUsers + Players)
/// </summary>
public class UserTreeService : IUserTreeService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<UserTreeService> _logger;

    public UserTreeService(CasinoDbContext context, ILogger<UserTreeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetUserTreeResponse?> GetUserTreeAsync(
        Guid userId,
        GetUserTreeRequest request,
        Guid? brandScope,
        Guid currentUserId,
        BackofficeUserRole currentRole)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Getting user tree for userId: {UserId}, MaxDepth: {MaxDepth}, CurrentUser: {CurrentUserId}, Role: {Role}",
            userId, request.MaxDepth, currentUserId, currentRole);

        // Primero buscar si el usuario existe como backoffice user
        var backofficeUser = await _context.BackofficeUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (backofficeUser != null)
        {
            // Validar acceso según scope
            if (currentRole != BackofficeUserRole.SUPER_ADMIN && backofficeUser.BrandId != brandScope)
            {
                _logger.LogWarning("Access denied: User {UserId} does not belong to brand scope {BrandScope}",
                    userId, brandScope);
                throw new InvalidOperationException("Access denied: User not in your scope");
            }

            // OPTIMIZACIÓN: Cargar TODOS los datos necesarios de una vez
            var allData = await LoadAllTreeDataAsync(userId, request, request.MaxDepth);
            
            var rootNode = BuildUserTreeNodeFromCache(userId, "BACKOFFICE", request, 0, allData);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("User tree loaded in {ElapsedMs}ms", elapsed);

            return new GetUserTreeResponse(
                backofficeUser.Id,
                backofficeUser.Username,
                "BACKOFFICE",
                backofficeUser.Role.ToString(),
                rootNode);
        }

        // Si no es backoffice, buscar como player
        var player = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId);

        if (player != null)
        {
            // Validar acceso según scope
            if (currentRole != BackofficeUserRole.SUPER_ADMIN && player.BrandId != brandScope)
            {
                _logger.LogWarning("Access denied: Player {PlayerId} does not belong to brand scope {BrandScope}",
                    userId, brandScope);
                throw new InvalidOperationException("Access denied: Player not in your scope");
            }

            // OPTIMIZACIÓN: Cargar TODOS los datos necesarios de una vez
            var allData = await LoadAllTreeDataAsync(userId, request, request.MaxDepth);
            
            var rootNode = BuildUserTreeNodeFromCache(userId, "PLAYER", request, 0, allData);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("User tree loaded in {ElapsedMs}ms", elapsed);

            return new GetUserTreeResponse(
                player.Id,
                player.Username,
                "PLAYER",
                null,
                rootNode);
        }

        // Usuario no encontrado
        _logger.LogWarning("User not found: {UserId}", userId);
        return null;
    }

    /// <summary>
    /// OPTIMIZACIÓN: Carga TODOS los datos del árbol en 2 queries
    /// Esto elimina el problema de N+1 queries
    /// </summary>
    private async Task<TreeDataCache> LoadAllTreeDataAsync(
        Guid rootUserId,
        GetUserTreeRequest request,
        int maxDepth)
    {
        var cache = new TreeDataCache();

        // IMPORTANTE: Cargar el usuario raíz primero
        var rootBackofficeUser = await _context.BackofficeUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == rootUserId);

        if (rootBackofficeUser != null)
        {
            cache.BackofficeUsers[rootUserId] = rootBackofficeUser;
        }
        else
        {
            var rootPlayer = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == rootUserId);

            if (rootPlayer != null)
            {
                cache.Players[rootUserId] = rootPlayer;
            }
        }

        // Cargar TODOS los usuarios necesarios recursivamente
        await LoadUsersRecursivelyAsync(new[] { rootUserId }, request, maxDepth, 0, cache);

        _logger.LogInformation("Loaded {BackofficeCount} backoffice users and {PlayerCount} players for tree",
            cache.BackofficeUsers.Count, cache.Players.Count);

        return cache;
    }

    /// <summary>
    /// Carga recursivamente todos los usuarios hasta la profundidad máxima
    /// </summary>
    private async Task LoadUsersRecursivelyAsync(
        IEnumerable<Guid> parentIds,
        GetUserTreeRequest request,
        int maxDepth,
        int currentDepth,
        TreeDataCache cache)
    {
        if (currentDepth > maxDepth || !parentIds.Any())
            return;

        var parentIdsList = parentIds.ToList();

        // Cargar BackofficeUsers hijos de estos padres
        var backofficeChildren = await _context.BackofficeUsers
            .AsNoTracking()
            .Where(u => parentIdsList.Contains(u.CreatedByUserId!.Value))
            .Where(u => request.IncludeInactive || u.Status == BackofficeUserStatus.ACTIVE)
            .ToListAsync();

        foreach (var user in backofficeChildren)
        {
            if (!cache.BackofficeUsers.ContainsKey(user.Id))
            {
                cache.BackofficeUsers[user.Id] = user;
            }
        }

        // Cargar Players hijos de estos padres
        var playerChildren = await _context.Players
            .AsNoTracking()
            .Where(p => parentIdsList.Contains(p.CreatedByUserId!.Value))
            .Where(p => request.IncludeInactive || p.Status == PlayerStatus.ACTIVE)
            .ToListAsync();

        foreach (var player in playerChildren)
        {
            if (!cache.Players.ContainsKey(player.Id))
            {
                cache.Players[player.Id] = player;
            }
        }

        // Cargar siguiente nivel
        var nextLevelIds = backofficeChildren.Select(u => u.Id)
            .Concat(playerChildren.Select(p => p.Id))
            .ToList();

        if (nextLevelIds.Any() && currentDepth < maxDepth)
        {
            await LoadUsersRecursivelyAsync(nextLevelIds, request, maxDepth, currentDepth + 1, cache);
        }
    }

    /// <summary>
    /// Construye el nodo del árbol usando datos pre-cargados (sin queries adicionales)
    /// </summary>
    private UserTreeNode BuildUserTreeNodeFromCache(
        Guid userId,
        string userType,
        GetUserTreeRequest request,
        int currentDepth,
        TreeDataCache cache)
    {
        // Obtener información del usuario desde cache
        string username;
        string? role = null;
        string status;
        DateTime createdAt;
        decimal balance = 0;
        decimal? commissionPercent = null;

        if (userType == "BACKOFFICE")
        {
            var user = cache.BackofficeUsers.GetValueOrDefault(userId);
            if (user == null)
                throw new InvalidOperationException($"BackofficeUser {userId} not found in cache");

            username = user.Username;
            role = user.Role.ToString();
            status = user.Status.ToString();
            createdAt = user.CreatedAt;
            balance = user.WalletBalance;
            
            if (user.Role == BackofficeUserRole.CASHIER && user.ParentCashierId.HasValue)
            {
                commissionPercent = user.CommissionPercent;
            }
        }
        else // PLAYER
        {
            var player = cache.Players.GetValueOrDefault(userId);
            if (player == null)
                throw new InvalidOperationException($"Player {userId} not found in cache");

            username = player.Username;
            status = player.Status.ToString();
            createdAt = player.CreatedAt;
            balance = player.WalletBalance;
        }

        // Contar y obtener hijos desde cache (sin queries)
        var backofficeChildren = cache.BackofficeUsers.Values
            .Where(u => u.CreatedByUserId == userId)
            .OrderBy(u => u.CreatedAt)
            .ToList();

        var playerChildren = cache.Players.Values
            .Where(p => p.CreatedByUserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToList();

        int totalChildrenCount = backofficeChildren.Count + playerChildren.Count;
        bool hasChildren = totalChildrenCount > 0;

        // Cargar hijos si no alcanzamos la profundidad máxima
        IEnumerable<UserTreeNode>? children = null;
        if (currentDepth < request.MaxDepth && hasChildren)
        {
            children = LoadChildrenFromCache(userId, request, currentDepth + 1, cache);
        }

        return new UserTreeNode(
            userId,
            username,
            userType,
            role,
            status,
            createdAt,
            balance,
            commissionPercent,
            hasChildren,
            totalChildrenCount,
            children);
    }

    /// <summary>
    /// Carga los hijos desde el cache (sin queries adicionales)
    /// </summary>
    private List<UserTreeNode> LoadChildrenFromCache(
        Guid parentUserId,
        GetUserTreeRequest request,
        int currentDepth,
        TreeDataCache cache)
    {
        var children = new List<UserTreeNode>();

        // Obtener hijos BackofficeUser desde cache
        var backofficeChildren = cache.BackofficeUsers.Values
            .Where(u => u.CreatedByUserId == parentUserId)
            .OrderBy(u => u.CreatedAt)
            .ToList();

        foreach (var child in backofficeChildren)
        {
            var childNode = BuildUserTreeNodeFromCache(child.Id, "BACKOFFICE", request, currentDepth, cache);
            children.Add(childNode);
        }

        // Obtener hijos Player desde cache
        var playerChildren = cache.Players.Values
            .Where(p => p.CreatedByUserId == parentUserId)
            .OrderBy(p => p.CreatedAt)
            .ToList();

        foreach (var child in playerChildren)
        {
            var childNode = BuildUserTreeNodeFromCache(child.Id, "PLAYER", request, currentDepth, cache);
            children.Add(childNode);
        }

        return children;
    }

    /// <summary>
    /// Cache de datos cargados para evitar N+1 queries
    /// </summary>
    private class TreeDataCache
    {
        public Dictionary<Guid, Domain.Entities.BackofficeUser> BackofficeUsers { get; } = new();
        public Dictionary<Guid, Domain.Entities.Player> Players { get; } = new();
    }
}

