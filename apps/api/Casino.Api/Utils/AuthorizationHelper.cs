using Casino.Application.Services;
using Casino.Domain.Enums;
using System.Security.Claims;

namespace Casino.Api.Utils;

/// <summary>
/// Helper para manejar la autorización y scoping basado en roles y brand context.
/// Sistema Brand-Only: todas las operaciones están scopeadas por brand (resuelto automáticamente por Host).
/// </summary>
public static class AuthorizationHelper
{
    /// <summary>
    /// Obtiene el ID del usuario actual desde el JWT token.
    /// </summary>
    public static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("Invalid user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// Obtiene el rol del usuario actual desde el JWT token.
    /// </summary>
    public static BackofficeUserRole GetCurrentUserRole(HttpContext httpContext)
    {
        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<BackofficeUserRole>(roleClaim, out var role))
        {
            throw new InvalidOperationException("Invalid role in token");
        }
        return role;
    }

    /// <summary>
    /// Obtiene el BrandId del usuario actual desde el JWT token (si está asignado a un brand).
    /// Solo relevante para roles BRAND_ADMIN y CASHIER. SUPER_ADMIN no tiene brand asignado.
    /// </summary>
    public static Guid? GetCurrentUserBrandId(HttpContext httpContext)
    {
        var brandIdClaim = httpContext.User.FindFirst("brand_id")?.Value;
        if (Guid.TryParse(brandIdClaim, out var brandId))
        {
            return brandId;
        }
        return null; // SUPER_ADMIN no tiene brand asignado
    }

    /// <summary>
    /// Valida el contexto de brand y permisos para una operación.
    /// REGLAS:
    /// - Todos los roles requieren BrandContext resuelto (excepto algunas operaciones específicas)
    /// - SUPER_ADMIN: acceso global, puede operar en cualquier brand
    /// - BRAND_ADMIN/CASHIER: debe coincidir su brand asignado con el brand del contexto
    /// </summary>
    public static IResult? ValidateBrandScopedOperation(
        BackofficeUserRole currentRole,
        Guid? currentUserBrandId,
        BrandContext brandContext,
        bool requireBrandContext = true,
        bool allowGlobalScope = false)
    {
        // Verificar que el brand context esté resuelto si es requerido
        if (requireBrandContext && !brandContext.IsResolved)
        {
            return Results.Problem(
                title: "Brand Not Resolved",
                detail: "Brand context is required for this operation. Please ensure the request is made to the correct domain.",
                statusCode: 400);
        }

        // SUPER_ADMIN tiene acceso global
        if (currentRole == BackofficeUserRole.SUPER_ADMIN)
        {
            return null; // Sin restricciones para SUPER_ADMIN
        }

        // Para BRAND_ADMIN y CASHIER: verificar que el brand del contexto coincida con su brand asignado
        if (brandContext.IsResolved && currentUserBrandId.HasValue)
        {
            if (currentUserBrandId.Value != brandContext.BrandId)
            {
                return Results.Problem(
                    title: "Brand Access Denied",
                    detail: $"User is assigned to a different brand. Current brand context: {brandContext.BrandCode}",
                    statusCode: 403);
            }
        }
        else if (currentRole != BackofficeUserRole.SUPER_ADMIN)
        {
            return Results.Problem(
                title: "User Not Assigned to Brand",
                detail: "User must be assigned to a brand to perform this operation",
                statusCode: 403);
        }

        return null; // Validación exitosa
    }

    /// <summary>
    /// Resuelve el BrandId efectivo para operaciones de creación/actualización.
    /// REGLAS:
    /// - SUPER_ADMIN: usa el brand del contexto actual (Host)
    /// - BRAND_ADMIN/CASHIER: debe usar su brand asignado (que debe coincidir con el contexto)
    /// </summary>
    public static Guid GetEffectiveBrandId(
        BackofficeUserRole currentRole,
        Guid? currentUserBrandId,
        BrandContext brandContext)
    {
        if (currentRole == BackofficeUserRole.SUPER_ADMIN)
        {
            // SUPER_ADMIN crea entidades en el brand del contexto actual (Host)
            if (!brandContext.IsResolved)
            {
                throw new InvalidOperationException("Brand context must be resolved for SUPER_ADMIN operations");
            }
            return brandContext.BrandId;
        }

        // BRAND_ADMIN y CASHIER usan su brand asignado
        if (!currentUserBrandId.HasValue)
        {
            throw new InvalidOperationException($"User with role {currentRole} must be assigned to a brand");
        }

        return currentUserBrandId.Value;
    }

    /// <summary>
    /// Resuelve el scope de consulta para listados.
    /// REGLAS:
    /// - SUPER_ADMIN: puede usar scope global (todos los brands) o brand específico
    /// - BRAND_ADMIN/CASHIER: solo pueden ver su brand asignado
    /// </summary>
    public static Guid? GetQueryScope(
        BackofficeUserRole currentRole,
        Guid? currentUserBrandId,
        BrandContext brandContext,
        bool requestedGlobalScope = false)
    {
        if (currentRole == BackofficeUserRole.SUPER_ADMIN)
        {
            if (requestedGlobalScope)
            {
                return null; // Scope global - ver todos los brands
            }
            else
            {
                // SUPER_ADMIN sin scope global: ver solo el brand del contexto actual
                return brandContext.IsResolved ? brandContext.BrandId : null;
            }
        }

        // BRAND_ADMIN y CASHIER: solo pueden ver su brand asignado
        return currentUserBrandId;
    }

    /// <summary>
    /// Valida permisos específicos para operaciones de usuarios.
    /// </summary>
    public static IResult? ValidateUserOperationPermissions(
        BackofficeUserRole currentRole,
        BackofficeUserRole? targetRole = null,
        BackofficeUserRole? newRole = null)
    {
        // Solo SUPER_ADMIN puede crear/modificar otros SUPER_ADMIN
        if ((targetRole == BackofficeUserRole.SUPER_ADMIN || newRole == BackofficeUserRole.SUPER_ADMIN) 
            && currentRole != BackofficeUserRole.SUPER_ADMIN)
        {
            return Results.Problem(
                title: "Insufficient Privileges",
                detail: "Only SUPER_ADMIN can create or modify SUPER_ADMIN users",
                statusCode: 403);
        }

        // BRAND_ADMIN puede crear/modificar CASHIER en su brand
        if (currentRole == BackofficeUserRole.BRAND_ADMIN)
        {
            if (targetRole.HasValue && targetRole.Value == BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Insufficient Privileges",
                    detail: "BRAND_ADMIN cannot modify SUPER_ADMIN users",
                    statusCode: 403);
            }

            if (newRole.HasValue && newRole.Value == BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Insufficient Privileges", 
                    detail: "BRAND_ADMIN cannot promote users to SUPER_ADMIN",
                    statusCode: 403);
            }
        }

        // CASHIER puede crear otros CASHIER (subordinados)
        if (currentRole == BackofficeUserRole.CASHIER)
        {
            if (newRole.HasValue && newRole.Value != BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Insufficient Privileges",
                    detail: "CASHIER can only create other CASHIER users",
                    statusCode: 403);
            }
            
            if (targetRole.HasValue && targetRole.Value != BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Insufficient Privileges",
                    detail: "CASHIER can only modify CASHIER users",
                    statusCode: 403);
            }
        }

        return null; // Sin errores
    }

    /// <summary>
    /// Obtiene información del contexto de autorización para logging y auditoría.
    /// </summary>
    public static object GetAuthorizationContext(
        HttpContext httpContext,
        BrandContext brandContext)
    {
        return new
        {
            UserId = GetCurrentUserId(httpContext),
            Role = GetCurrentUserRole(httpContext).ToString(),
            UserBrandId = GetCurrentUserBrandId(httpContext),
            ContextBrandId = brandContext.IsResolved ? brandContext.BrandId : (Guid?)null,
            BrandCode = brandContext.BrandCode,
            Domain = brandContext.Domain,
            RequestIP = httpContext.Connection.RemoteIpAddress?.ToString()
        };
    }

    // === LEGACY METHODS FOR COMPATIBILITY (DEPRECATED) ===
    // These methods are kept for compatibility with old endpoints
    // TODO: Remove after migration is complete

    /// <summary>
    /// Legacy method for compatibility - use ValidateUserOperationPermissions instead
    /// </summary>
    [Obsolete("Use ValidateUserOperationPermissions instead")]
    public static IResult? ValidateOperationPermissions(
        BackofficeUserRole currentRole,
        BackofficeUserRole? targetRole = null,
        BackofficeUserRole? newRole = null)
    {
        return ValidateUserOperationPermissions(currentRole, targetRole, newRole);
    }

    /// <summary>
    /// Legacy method for compatibility - use GetQueryScope instead  
    /// </summary>
    [Obsolete("Use GetQueryScope instead")]
    public static Guid? GetEffectiveBrandScope(
        BackofficeUserRole currentRole,
        Guid? currentUserBrandId,
        BrandContext brandContext,
        bool requestedGlobalScope = false)
    {
        return GetQueryScope(currentRole, currentUserBrandId, brandContext, requestedGlobalScope);
    }
}