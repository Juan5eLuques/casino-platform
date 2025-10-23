using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class HierarchyService : IHierarchyService
{
    private readonly CasinoDbContext _db;
    private readonly ILogger<HierarchyService> _logger;
    
    public HierarchyService(
        CasinoDbContext db,
        ILogger<HierarchyService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<IEnumerable<BackofficeUser>> GetDescendantsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var user = await _db.BackofficeUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null || string.IsNullOrEmpty(user.HierarchyPath))
        {
            _logger.LogWarning("User {UserId} not found or has no hierarchy_path", userId);
            return Enumerable.Empty<BackofficeUser>();
        }
        
        // Query eficiente usando hierarchy_path
        // Ejemplo: Si user.HierarchyPath = ".root.admin1."
        // Busca todos los que empiecen con ".root.admin1." (sus descendientes)
        var descendants = await _db.BackofficeUsers
            .AsNoTracking()
            .Where(u => u.HierarchyPath != null 
                     && u.HierarchyPath.StartsWith(user.HierarchyPath) 
                     && u.Id != userId  // Excluir al usuario mismo
                     && u.Status == BackofficeUserStatus.ACTIVE)
            .OrderBy(u => u.HierarchyLevel)
            .ThenBy(u => u.Username)
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation(
            "Found {Count} descendants for user {UserId} at level {Level}",
            descendants.Count, userId, user.HierarchyLevel);
        
        return descendants;
    }
    
    public async Task<IEnumerable<BackofficeUser>> GetAncestorsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var ancestors = new List<BackofficeUser>();
        Guid? currentId = userId;
        var maxDepth = 10;  // Prevenir loops infinitos
        var depth = 0;
        
        while (currentId.HasValue && depth < maxDepth)
        {
            var user = await _db.BackofficeUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentId.Value, cancellationToken);
            
            if (user == null) break;
            
            ancestors.Add(user);
            
            // Subir al padre (usar ParentAdminId primero, fallback a ParentCashierId)
            currentId = user.ParentAdminId ?? user.ParentCashierId;
            depth++;
        }
        
        if (depth >= maxDepth)
        {
            _logger.LogWarning(
                "Max depth reached while traversing ancestors for user {UserId}", 
                userId);
        }
        
        _logger.LogInformation(
            "Found {Count} ancestors for user {UserId}",
            ancestors.Count, userId);
        
        return ancestors;
    }
    
    public async Task<bool> CanOperateOnAsync(
        Guid actorId, 
        Guid targetId, 
        CancellationToken cancellationToken = default)
    {
        // Mismo usuario siempre puede operar sobre sí mismo
        if (actorId == targetId) return true;
        
        var actor = await _db.BackofficeUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
        
        if (actor == null)
        {
            _logger.LogWarning("Actor {ActorId} not found", actorId);
            return false;
        }
        
        // SUPER_ADMIN puede operar sobre cualquiera
        if (actor.Role == BackofficeUserRole.SUPER_ADMIN)
        {
            _logger.LogInformation("SUPER_ADMIN {ActorId} has permission", actorId);
            return true;
        }
        
        var target = await _db.BackofficeUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetId, cancellationToken);
        
        if (target == null)
        {
            _logger.LogWarning("Target {TargetId} not found", targetId);
            return false;
        }
        
        // Validar que target esté en el árbol del actor (descendiente)
        // Usar hierarchy_path para validación eficiente
        if (string.IsNullOrEmpty(actor.HierarchyPath) || 
            string.IsNullOrEmpty(target.HierarchyPath))
        {
            _logger.LogWarning(
                "Actor {ActorId} or Target {TargetId} has no hierarchy_path",
                actorId, targetId);
            return false;
        }
        
        bool isDescendant = target.HierarchyPath.StartsWith(actor.HierarchyPath);
        
        _logger.LogInformation(
            "Permission check: Actor {ActorId} (path: {ActorPath}) ? Target {TargetId} (path: {TargetPath}) = {Result}",
            actorId, actor.HierarchyPath, targetId, target.HierarchyPath, isDescendant);
        
        return isDescendant;
    }
    
    public async Task<string> CalculateHierarchyPathAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var user = await _db.BackofficeUsers
            .Include(u => u.ParentAdmin)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }
        
        string newPath;
        int newLevel;
        
        if (user.ParentAdminId == null)
        {
            // Es raíz (SUPER_ADMIN o sin padre)
            newPath = ".root.";
            newLevel = 0;
        }
        else
        {
            // Tiene padre, concatenar path
            var parent = user.ParentAdmin ?? await _db.BackofficeUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.ParentAdminId, cancellationToken);
            
            if (parent == null)
            {
                _logger.LogWarning(
                    "Parent {ParentId} not found for user {UserId}",
                    user.ParentAdminId, userId);
                newPath = $".root.{userId}.";
                newLevel = 1;
            }
            else
            {
                newPath = parent.HierarchyPath + user.Id + ".";
                newLevel = parent.HierarchyLevel + 1;
            }
        }
        
        // Actualizar usuario
        user.HierarchyPath = newPath;
        user.HierarchyLevel = newLevel;
        
        await _db.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Updated hierarchy for user {UserId}: path={Path}, level={Level}",
            userId, newPath, newLevel);
        
        return newPath;
    }
    
    public async Task<IEnumerable<BackofficeUser>> GetUsersByLevelAsync(
        int level, 
        Guid? brandId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _db.BackofficeUsers
            .AsNoTracking()
            .Where(u => u.HierarchyLevel == level 
                     && u.Status == BackofficeUserStatus.ACTIVE);
        
        if (brandId.HasValue)
        {
            query = query.Where(u => u.BrandId == brandId.Value);
        }
        
        var users = await query
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation(
            "Found {Count} users at level {Level} for brand {BrandId}",
            users.Count, level, brandId);
        
        return users;
    }
}
