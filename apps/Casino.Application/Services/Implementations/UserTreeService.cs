using Casino.Application.DTOs.Admin;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// Servicio para gestionar el árbol genealógico de usuarios
/// Permite ver qué usuarios fueron creados por otros usuarios de forma jerárquica
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

            var rootNode = await BuildUserTreeNodeAsync(userId, "BACKOFFICE", request, 0);

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

            var rootNode = await BuildUserTreeNodeAsync(userId, "PLAYER", request, 0);

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
    /// Construye recursivamente un nodo del árbol con sus hijos
    /// </summary>
    private async Task<UserTreeNode> BuildUserTreeNodeAsync(
        Guid userId,
        string userType,
        GetUserTreeRequest request,
        int currentDepth)
    {
        // Obtener información del usuario
        string username;
        string? role = null;
        string status;
        DateTime createdAt;

        if (userType == "BACKOFFICE")
        {
            var user = await _context.BackofficeUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException($"BackofficeUser {userId} not found");

            username = user.Username;
            role = user.Role.ToString();
            status = user.Status.ToString();
            createdAt = user.CreatedAt;
        }
        else // PLAYER
        {
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == userId);

            if (player == null)
                throw new InvalidOperationException($"Player {userId} not found");

            username = player.Username;
            status = player.Status.ToString();
            createdAt = player.CreatedAt;
        }

        // Contar hijos directos (usuarios creados por este usuario)
        var backofficeChildrenCount = await _context.BackofficeUsers
            .Where(u => u.CreatedByUserId == userId)
            .Where(u => request.IncludeInactive || u.Status == BackofficeUserStatus.ACTIVE)
            .CountAsync();

        var playerChildrenCount = await _context.Players
            .Where(p => p.CreatedByUserId == userId)
            .Where(p => request.IncludeInactive || p.Status == PlayerStatus.ACTIVE)
            .CountAsync();

        int totalChildrenCount = backofficeChildrenCount + playerChildrenCount;
        bool hasChildren = totalChildrenCount > 0;

        // Si ya alcanzamos la profundidad máxima, no cargar hijos
        IEnumerable<UserTreeNode>? children = null;
        if (currentDepth < request.MaxDepth && hasChildren)
        {
            children = await LoadChildrenAsync(userId, request, currentDepth + 1);
        }

        return new UserTreeNode(
            userId,
            username,
            userType,
            role,
            status,
            createdAt,
            hasChildren,
            totalChildrenCount,
            children);
    }

    /// <summary>
    /// Carga los hijos directos de un usuario
    /// </summary>
    private async Task<List<UserTreeNode>> LoadChildrenAsync(
        Guid parentUserId,
        GetUserTreeRequest request,
        int currentDepth)
    {
        var children = new List<UserTreeNode>();

        // Cargar hijos de tipo BackofficeUser
        var backofficeChildren = await _context.BackofficeUsers
            .AsNoTracking()
            .Where(u => u.CreatedByUserId == parentUserId)
            .Where(u => request.IncludeInactive || u.Status == BackofficeUserStatus.ACTIVE)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        foreach (var child in backofficeChildren)
        {
            var childNode = await BuildUserTreeNodeAsync(child.Id, "BACKOFFICE", request, currentDepth);
            children.Add(childNode);
        }

        // Cargar hijos de tipo Player
        var playerChildren = await _context.Players
            .AsNoTracking()
            .Where(p => p.CreatedByUserId == parentUserId)
            .Where(p => request.IncludeInactive || p.Status == PlayerStatus.ACTIVE)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        foreach (var child in playerChildren)
        {
            var childNode = await BuildUserTreeNodeAsync(child.Id, "PLAYER", request, currentDepth);
            children.Add(childNode);
        }

        _logger.LogDebug("Loaded {Count} children for user {ParentUserId} at depth {Depth}",
            children.Count, parentUserId, currentDepth);

        return children;
    }
}
