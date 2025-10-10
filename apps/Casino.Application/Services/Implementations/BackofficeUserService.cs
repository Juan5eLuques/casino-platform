using Casino.Application.DTOs.Admin;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class BackofficeUserService : IBackofficeUserService
{
    private readonly CasinoDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BackofficeUserService> _logger;

    public BackofficeUserService(
        CasinoDbContext context,
        IPasswordService passwordService,
        IAuditService auditService,
        ILogger<BackofficeUserService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetBackofficeUserResponse> CreateUserAsync(CreateBackofficeUserRequest request, Guid currentUserId, Guid? effectiveBrandId)
    {
        // SONNET: Obtener el rol del usuario actual para auditoría
        var currentUser = await _context.BackofficeUsers
            .FirstOrDefaultAsync(u => u.Id == currentUserId);
        
        if (currentUser == null)
        {
            throw new InvalidOperationException("Current user not found");
        }

        // Verificar que el username no esté en uso
        BackofficeUser? existingUser = null;
        
        if (request.Role == BackofficeUserRole.SUPER_ADMIN)
        {
            // SUPER_ADMIN: username único globalmente (solo entre SUPER_ADMINs)
            existingUser = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.BrandId == null);
        }
        else
        {
            // BRAND_ADMIN/CASHIER: username único por brand
            if (!effectiveBrandId.HasValue)
            {
                throw new InvalidOperationException("Brand ID is required for non-SUPER_ADMIN users");
            }
            
            existingUser = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.BrandId == effectiveBrandId);
        }

        if (existingUser != null)
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists in this brand");
        }

        // Validar brand para roles no-SUPER_ADMIN
        Guid? assignedBrandId = null;
        Brand? targetBrand = null;

        if (request.Role == BackofficeUserRole.SUPER_ADMIN)
        {
            // SUPER_ADMIN no se asigna a ningún brand
            assignedBrandId = null;
        }
        else
        {
            // BRAND_ADMIN/CASHIER se asignan al brand del contexto
            if (!effectiveBrandId.HasValue)
            {
                throw new InvalidOperationException("Brand ID is required for non-SUPER_ADMIN users");
            }
            
            assignedBrandId = effectiveBrandId.Value;
            
            targetBrand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == effectiveBrandId.Value && b.Status == BrandStatus.ACTIVE);

            if (targetBrand == null)
            {
                throw new InvalidOperationException("Target brand not found or inactive");
            }
        }

        // Validar jerarquía de cashiers
        BackofficeUser? parentCashier = null;
        if (request.ParentCashierId.HasValue)
        {
            if (request.Role != BackofficeUserRole.CASHIER)
            {
                throw new InvalidOperationException("Only CASHIER role can have a parent cashier");
            }

            parentCashier = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Id == request.ParentCashierId.Value 
                    && u.Role == BackofficeUserRole.CASHIER 
                    && u.Status == BackofficeUserStatus.ACTIVE
                    && u.BrandId == assignedBrandId); // Debe estar en el mismo brand

            if (parentCashier == null)
            {
                throw new InvalidOperationException("Parent cashier not found, inactive, or belongs to a different brand");
            }
        }

        // Validar comisión - SONNET: Actualizado para CommissionPercent
        if (request.CommissionRate < 0 || request.CommissionRate > 100)
        {
            throw new InvalidOperationException("Commission rate must be between 0 and 100");
        }

        if (request.Role != BackofficeUserRole.CASHIER && request.CommissionRate > 0)
        {
            throw new InvalidOperationException("Only CASHIER role can have commission rate");
        }

        var passwordHash = _passwordService.HashPassword(request.Password);

        var newUser = new BackofficeUser
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = passwordHash,
            Role = request.Role,
            BrandId = assignedBrandId,
            ParentCashierId = request.ParentCashierId,
            CommissionPercent = request.Role == BackofficeUserRole.CASHIER ? request.CommissionRate : 0,
            Status = BackofficeUserStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            // SONNET: Guardar auditoría de creación
            CreatedByUserId = currentUserId,
            CreatedByRole = currentUser.Role.ToString()
        };

        _context.BackofficeUsers.Add(newUser);
        await _context.SaveChangesAsync();

        // Cargar relaciones para la respuesta
        if (newUser.BrandId.HasValue)
        {
            await _context.Entry(newUser)
                .Reference(u => u.Brand)
                .LoadAsync();
        }
        
        if (newUser.ParentCashierId.HasValue)
        {
            await _context.Entry(newUser)
                .Reference(u => u.ParentCashier)
                .LoadAsync();
        }

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "CREATE_USER",
            "BackofficeUser",
            newUser.Id.ToString(),
            new { 
                request.Username, 
                request.Role, 
                BrandId = assignedBrandId,
                BrandName = targetBrand?.Name,
                ParentCashierId = request.ParentCashierId,
                ParentCashierUsername = newUser.ParentCashier?.Username,
                CommissionPercent = newUser.CommissionPercent,
                CreatedByUserId = currentUserId,
                CreatedByRole = currentUser.Role.ToString()
            });

        _logger.LogInformation("Backoffice user created: {UserId} - {Username} - {Role} in brand {BrandId} by user {CreatedByUserId} ({CreatedByRole})",
            newUser.Id, newUser.Username, newUser.Role, assignedBrandId, currentUserId, currentUser.Role);

        return new GetBackofficeUserResponse(
            newUser.Id,
            newUser.Username,
            newUser.Role,
            newUser.Status,
            newUser.BrandId,
            newUser.Brand?.Name,
            newUser.ParentCashierId,
            newUser.ParentCashier?.Username,
            newUser.CommissionPercent,
            0, // SubordinatesCount se calcula en consultas
            newUser.CreatedAt,
            newUser.LastLoginAt);
    }

    public async Task<QueryBackofficeUsersResponse> GetUsersAsync(QueryBackofficeUsersRequest request, Guid? brandScope = null, Guid? currentUserId = null, BackofficeUserRole? currentUserRole = null)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Brand)
            .Include(u => u.ParentCashier)
            .AsQueryable();

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            // Scope específico por brand
            query = query.Where(u => u.BrandId == brandScope.Value);
        }
        else if (!request.GlobalScope)
        {
            // Si no hay scope específico y no es global, filtrar por brand actual
            // Esto no debería pasar en el nuevo sistema, pero mantener por seguridad
            return new QueryBackofficeUsersResponse(
                Enumerable.Empty<GetBackofficeUserResponse>(),
                0, request.Page, request.PageSize, 0);
        }
        // Si brandScope es null y GlobalScope es true, no aplicar filtro (ver todos)

        // Para CASHIER: aplicar filtro de jerarquía (solo ver él mismo y subordinados)
        if (currentUserRole == BackofficeUserRole.CASHIER && currentUserId.HasValue)
        {
            // Obtener IDs de la jerarquía del cashier (él mismo y todos sus subordinados recursivamente)
            var hierarchyIds = await GetCashierHierarchyIdsAsync(currentUserId.Value);
            query = query.Where(u => hierarchyIds.Contains(u.Id));
        }

        // Aplicar filtros adicionales
        if (!string.IsNullOrEmpty(request.Username))
        {
            query = query.Where(u => u.Username.Contains(request.Username));
        }

        if (request.Role.HasValue)
        {
            query = query.Where(u => u.Role == request.Role.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        if (request.ParentCashierId.HasValue)
        {
            query = query.Where(u => u.ParentCashierId == request.ParentCashierId.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Role) // SUPER_ADMIN primero, luego BRAND_ADMIN, luego CASHIER
            .ThenBy(u => u.Brand != null ? u.Brand.Name : "")
            .ThenBy(u => u.Username)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Calcular subordinates count para cada usuario
        var userResponses = new List<GetBackofficeUserResponse>();
        foreach (var user in users)
        {
            var subordinatesCount = await _context.BackofficeUsers
                .CountAsync(u => u.ParentCashierId == user.Id);

            userResponses.Add(new GetBackofficeUserResponse(
                user.Id,
                user.Username,
                user.Role,
                user.Status,
                user.BrandId,
                user.Brand?.Name,
                user.ParentCashierId,
                user.ParentCashier?.Username,
                user.CommissionPercent,
                subordinatesCount,
                user.CreatedAt,
                user.LastLoginAt));
        }

        return new QueryBackofficeUsersResponse(
            userResponses,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }

    /// <summary>
    /// Obtiene recursivamente todos los IDs de la jerarquía de un cashier (él mismo y subordinados)
    /// </summary>
    private async Task<List<Guid>> GetCashierHierarchyIdsAsync(Guid cashierId)
    {
        var result = new List<Guid> { cashierId }; // Incluir al cashier mismo

        // Obtener subordinados directos
        var directSubordinates = await _context.BackofficeUsers
            .Where(u => u.ParentCashierId == cashierId)
            .Select(u => u.Id)
            .ToListAsync();

        // Recursivamente obtener subordinados de los subordinados
        foreach (var subordinateId in directSubordinates)
        {
            var subordinateHierarchy = await GetCashierHierarchyIdsAsync(subordinateId);
            result.AddRange(subordinateHierarchy);
        }

        return result.Distinct().ToList();
    }

    public async Task<GetBackofficeUserResponse?> GetUserAsync(Guid userId, Guid? brandScope = null)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Brand)
            .Include(u => u.ParentCashier)
            .AsQueryable();

        // Aplicar scope por brand si no es SUPER_ADMIN
        if (brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var subordinatesCount = await _context.BackofficeUsers
            .CountAsync(u => u.ParentCashierId == user.Id);

        return new GetBackofficeUserResponse(
            user.Id,
            user.Username,
            user.Role,
            user.Status,
            user.BrandId,
            user.Brand?.Name,
            user.ParentCashierId,
            user.ParentCashier?.Username,
            user.CommissionPercent,
            subordinatesCount,
            user.CreatedAt,
            user.LastLoginAt);
    }

    public async Task<GetBackofficeUserHierarchyResponse?> GetUserHierarchyAsync(Guid userId, Guid? brandScope = null)
    {
        var query = _context.BackofficeUsers.AsQueryable();

        // Aplicar scope por brand si no es SUPER_ADMIN
        if (brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return await BuildUserHierarchyAsync(user, brandScope);
    }

    private async Task<GetBackofficeUserHierarchyResponse> BuildUserHierarchyAsync(BackofficeUser user, Guid? brandScope)
    {
        var subordinatesQuery = _context.BackofficeUsers
            .Where(u => u.ParentCashierId == user.Id);

        if (brandScope.HasValue)
        {
            subordinatesQuery = subordinatesQuery.Where(u => u.BrandId == brandScope.Value);
        }

        var subordinates = await subordinatesQuery.ToListAsync();

        var subordinateResponses = new List<GetBackofficeUserHierarchyResponse>();
        foreach (var subordinate in subordinates)
        {
            var subordinateHierarchy = await BuildUserHierarchyAsync(subordinate, brandScope);
            subordinateResponses.Add(subordinateHierarchy);
        }

        return new GetBackofficeUserHierarchyResponse(
            user.Id,
            user.Username,
            user.Role,
            user.Status,
            user.ParentCashierId,
            user.CommissionPercent,
            user.CreatedAt,
            subordinateResponses);
    }

    public async Task<GetBackofficeUserResponse> UpdateUserAsync(Guid userId, UpdateBackofficeUserRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Brand)
            .Include(u => u.ParentCashier)
            .AsQueryable();

        // Aplicar scope por brand si no es SUPER_ADMIN
        if (brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found or access denied");
        }

        var changes = new Dictionary<string, object>();

        // Cambio de username
        if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
        {
            var existingUser = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != userId);

            if (existingUser != null)
            {
                throw new InvalidOperationException($"Username '{request.Username}' already exists");
            }

            changes["Username"] = new { Old = user.Username, New = request.Username };
            user.Username = request.Username;
        }

        // Cambio de password
        if (!string.IsNullOrEmpty(request.Password))
        {
            var newPasswordHash = _passwordService.HashPassword(request.Password);
            changes["Password"] = "Changed";
            user.PasswordHash = newPasswordHash;
        }

        // Cambio de rol
        if (request.Role.HasValue && request.Role.Value != user.Role)
        {
            // Validaciones específicas de rol
            if (request.Role.Value == BackofficeUserRole.SUPER_ADMIN && user.BrandId.HasValue)
            {
                throw new InvalidOperationException("Cannot change user to SUPER_ADMIN while assigned to a brand");
            }

            // Si cambia de CASHIER a otro rol, limpiar jerarquía
            if (user.Role == BackofficeUserRole.CASHIER && request.Role.Value != BackofficeUserRole.CASHIER)
            {
                var hasSubordinates = await _context.BackofficeUsers
                    .AnyAsync(u => u.ParentCashierId == user.Id);

                if (hasSubordinates)
                {
                    throw new InvalidOperationException("Cannot change role: user has subordinate cashiers");
                }

                user.ParentCashierId = null;
                user.CommissionPercent = 0;
            }

            changes["Role"] = new { Old = user.Role, New = request.Role.Value };
            user.Role = request.Role.Value;
        }

        // Cambio de status
        if (request.Status.HasValue && request.Status.Value != user.Status)
        {
            changes["Status"] = new { Old = user.Status, New = request.Status.Value };
            user.Status = request.Status.Value;
        }

        // Cambio de comisión
        if (request.CommissionRate.HasValue && request.CommissionRate.Value != user.CommissionPercent)
        {
            if (request.CommissionRate.Value < 0 || request.CommissionRate.Value > 100)
            {
                throw new InvalidOperationException("Commission rate must be between 0 and 100");
            }

            if (user.Role != BackofficeUserRole.CASHIER && request.CommissionRate.Value > 0)
            {
                throw new InvalidOperationException("Only CASHIER role can have commission rate");
            }

            changes["CommissionPercent"] = new { Old = user.CommissionPercent, New = request.CommissionRate.Value };
            user.CommissionPercent = request.CommissionRate.Value;
        }

        if (changes.Any())
        {
            await _context.SaveChangesAsync();

            // Recargar relaciones si cambiaron
            await _context.Entry(user)
                .Reference(u => u.Brand)
                .LoadAsync();
            
            if (user.ParentCashierId.HasValue)
            {
                await _context.Entry(user)
                    .Reference(u => u.ParentCashier)
                    .LoadAsync();
            }

            await _auditService.LogBackofficeActionAsync(
                currentUserId,
                "UPDATE_USER",
                "BackofficeUser",
                user.Id.ToString(),
                changes);

            _logger.LogInformation("Backoffice user updated: {UserId} by user {UpdatedByUserId}",
                user.Id, currentUserId);
        }

        var subordinatesCount = await _context.BackofficeUsers
            .CountAsync(u => u.ParentCashierId == user.Id);

        return new GetBackofficeUserResponse(
            user.Id,
            user.Username,
            user.Role,
            user.Status,
            user.BrandId,
            user.Brand?.Name,
            user.ParentCashierId,
            user.ParentCashier?.Username,
            user.CommissionPercent,
            subordinatesCount,
            user.CreatedAt,
            user.LastLoginAt);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, Guid currentUserId, Guid? brandScope = null)
    {
        var query = _context.BackofficeUsers.AsQueryable();

        // Aplicar scope por brand si no es SUPER_ADMIN
        if (brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        // No permitir eliminar al propio usuario
        if (user.Id == currentUserId)
        {
            throw new InvalidOperationException("Cannot delete your own user account");
        }

        // No permitir eliminar SUPER_ADMIN si tienes scope de brand
        if (user.Role == BackofficeUserRole.SUPER_ADMIN && brandScope.HasValue)
        {
            throw new InvalidOperationException("Access denied: cannot delete SUPER_ADMIN users");
        }

        // No permitir eliminar cashier con subordinados
        if (user.Role == BackofficeUserRole.CASHIER)
        {
            var hasSubordinates = await _context.BackofficeUsers
                .AnyAsync(u => u.ParentCashierId == user.Id);

            if (hasSubordinates)
            {
                throw new InvalidOperationException("Cannot delete cashier with subordinates");
            }
        }

        _context.BackofficeUsers.Remove(user);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "DELETE_USER",
            "BackofficeUser",
            user.Id.ToString(),
            new { user.Username, user.Role, DeletedAt = DateTime.UtcNow });

        _logger.LogInformation("Backoffice user deleted: {UserId} by user {DeletedByUserId}",
            user.Id, currentUserId);

        return true;
    }
}