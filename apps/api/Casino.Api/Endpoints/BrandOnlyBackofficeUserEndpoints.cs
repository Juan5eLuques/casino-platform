using Casino.Api.Utils;
using Casino.Application.DTOs.Admin;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

// DTOs para requests específicos de endpoints
public record UpdateUserStatusRequest(BackofficeUserStatus Status);
public record UpdateUserRoleRequest(BackofficeUserRole Role);

public static class BrandOnlyBackofficeUserEndpoints
{
    public static void MapBrandOnlyBackofficeUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Backoffice Users (Brand-Only)");

        // === CREATE USER ===
        group.MapPost("/users", CreateBackofficeUser)
            .RequireAuthorization("AdminOrSuperAdmin") // CASHIER no puede crear usuarios
            .WithName("CreateBackofficeUserBrandOnly")
            .WithSummary("Create backoffice user (brand resolved by Host)")
            .WithDescription("Creates a new backoffice user. Brand is automatically resolved from Host. SUPER_ADMIN users are not assigned to any brand.")
            .Produces<GetBackofficeUserResponse>(201)
            .Produces(400) // brand_not_resolved, validation errors
            .Produces(403) // insufficient privileges
            .Produces(409); // username_exists

        // === LIST USERS ===
        group.MapGet("/users", ListBackofficeUsers)
            .RequireAuthorization("AnyBackofficeUser") // Todos los roles pueden listar usuarios (con sus restricciones)
            .WithName("ListBackofficeUsersBrandOnly")
            .WithSummary("List backoffice users (brand scoped)")
            .WithDescription("Lists users scoped to current brand. SUPER_ADMIN can use ?globalScope=true to see all brands.")
            .Produces<QueryBackofficeUsersResponse>()
            .Produces(400) // brand_not_resolved
            .Produces(403); // access denied

        // === GET USER ===
        group.MapGet("/users/{userId:guid}", GetBackofficeUser)
            .RequireAuthorization("AnyBackofficeUser") // Todos los roles pueden ver usuarios (con sus restricciones)
            .WithName("GetBackofficeUserBrandOnly")
            .WithSummary("Get backoffice user details")
            .Produces<GetBackofficeUserResponse>()
            .Produces(404)
            .Produces(403);

        // === UPDATE USER STATUS ===
        group.MapPatch("/users/{userId:guid}/status", UpdateUserStatus)
            .RequireAuthorization("AdminOrSuperAdmin")
            .WithName("UpdateBackofficeUserStatus")
            .WithSummary("Update user status (active/inactive)")
            .Produces<GetBackofficeUserResponse>()
            .Produces(404)
            .Produces(403);

        // === UPDATE USER ROLE ===
        group.MapPatch("/users/{userId:guid}/role", UpdateUserRole)
            .RequireAuthorization("SuperAdminOnly") // Solo SUPER_ADMIN puede cambiar roles
            .WithName("UpdateBackofficeUserRole")
            .WithSummary("Update user role (SUPER_ADMIN only)")
            .Produces<GetBackofficeUserResponse>()
            .Produces(404)
            .Produces(403);

        // === DELETE USER ===
        group.MapDelete("/users/{userId:guid}", DeleteBackofficeUser)
            .RequireAuthorization("AdminOrSuperAdmin")
            .WithName("DeleteBackofficeUserBrandOnly")
            .WithSummary("Delete backoffice user")
            .Produces(200)
            .Produces(404)
            .Produces(403)
            .Produces(409); // has subordinates

        // === CASHIER ENDPOINTS (ESPECÍFICOS PARA CAJEROS) ===
        group.MapPost("/cashiers", CreateCashier)
            .RequireAuthorization("AnyBackofficeUser") // CASHIER puede crear otros CASHIER
            .WithName("CreateCashierBrandOnly")
            .WithSummary("Create cashier (brand resolved by Host)")
            .WithDescription("Creates a new cashier. CASHIER can create subordinate cashiers. BRAND_ADMIN/SUPER_ADMIN can create any cashier.")
            .Produces<GetBackofficeUserResponse>(201)
            .Produces(400)
            .Produces(403)
            .Produces(409);
    }

    private static async Task<IResult> CreateBackofficeUser(
        [FromBody] CreateBackofficeUserRequest request,
        IBackofficeUserService backofficeUserService,
        IValidator<CreateBackofficeUserRequest> validator,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyBackofficeUserEndpoints");

        // Validar request
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar permisos específicos para creación de usuarios
            var userPermissionValidation = AuthorizationHelper.ValidateUserOperationPermissions(
                currentRole, newRole: request.Role);
            if (userPermissionValidation != null) return userPermissionValidation;

            // CASHIER no puede crear usuarios usando este endpoint
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER should use /api/v1/admin/cashiers endpoint to create subordinate cashiers",
                    statusCode: 403);
            }

            // Resolver brand efectivo para el nuevo usuario
            Guid? effectiveBrandId = null;
            
            if (request.Role == BackofficeUserRole.SUPER_ADMIN)
            {
                // SUPER_ADMIN no se asigna a ningún brand (BrandId = NULL)
                effectiveBrandId = null;
            }
            else
            {
                // BRAND_ADMIN/CASHIER: validar brand context y usar brand del contexto
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;

                effectiveBrandId = AuthorizationHelper.GetEffectiveBrandId(
                    currentRole, currentUserBrandId, brandContext);
            }

            // Crear usuario
            var response = await backofficeUserService.CreateUserAsync(request, currentUserId, effectiveBrandId);

            logger.LogInformation("User created: {UserId} by {CurrentUserId} in brand {BrandId}",
                response.Id, currentUserId, effectiveBrandId);

            return Results.Created($"/api/v1/admin/users/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("User creation failed: {Error}", ex.Message);
            
            if (ex.Message.Contains("already exists"))
                return Results.Conflict(new { error = "username_exists", message = ex.Message });
            
            return Results.Problem(
                title: "User Creation Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating user");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> CreateCashier(
        [FromBody] CreateBackofficeUserRequest request,
        IBackofficeUserService backofficeUserService,
        IValidator<CreateBackofficeUserRequest> validator,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyBackofficeUserEndpoints");

        // Validar request
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        // Forzar que solo se puedan crear CASHIER
        if (request.Role != BackofficeUserRole.CASHIER)
        {
            return Results.Problem(
                title: "Invalid Role",
                detail: "This endpoint only allows creating CASHIER users",
                statusCode: 400);
        }

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context
            var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
            if (brandValidation != null) return brandValidation;

            // Resolver brand efectivo
            var effectiveBrandId = AuthorizationHelper.GetEffectiveBrandId(
                currentRole, currentUserBrandId, brandContext);

            // Para CASHIER: auto-asignar como parent
            CreateBackofficeUserRequest finalRequest = request;
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                finalRequest = request with { ParentCashierId = currentUserId };
                logger.LogInformation("CASHIER {CashierId} creating subordinate cashier with auto-assignment", currentUserId);
            }

            // Crear usuario
            var response = await backofficeUserService.CreateUserAsync(finalRequest, currentUserId, effectiveBrandId);

            logger.LogInformation("Cashier created: {UserId} by {CurrentUserId} in brand {BrandId}",
                response.Id, currentUserId, effectiveBrandId);

            return Results.Created($"/api/v1/admin/users/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Cashier creation failed: {Error}", ex.Message);
            
            if (ex.Message.Contains("already exists"))
                return Results.Conflict(new { error = "username_exists", message = ex.Message });
            
            return Results.Problem(
                title: "Cashier Creation Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating cashier");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> ListBackofficeUsers(
        [AsParameters] QueryBackofficeUsersRequest request,
        IBackofficeUserService backofficeUserService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyBackofficeUserEndpoints");

        try
        {
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar permisos para scope global (solo SUPER_ADMIN)
            if (request.GlobalScope && currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN can use global scope",
                    statusCode: 403);
            }

            // Para SUPER_ADMIN con global scope, no validar brand context
            // Para los demás casos, validar brand context
            if (!request.GlobalScope || currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            // Resolver scope de consulta
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext, request.GlobalScope);

            // Llamar al servicio con información del usuario actual para filtrado de jerarquía
            var response = await backofficeUserService.GetUsersAsync(
                request, queryScope, currentUserId, currentRole);

            logger.LogInformation("Listed {Count} users for role {Role} with scope {Scope}",
                response.TotalCount, currentRole, queryScope?.ToString() ?? "global");

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing users");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> GetBackofficeUser(
        Guid userId,
        IBackofficeUserService backofficeUserService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyBackofficeUserEndpoints");

        try
        {
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context para roles no-SUPER_ADMIN
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            // Resolver scope
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var user = await backofficeUserService.GetUserAsync(userId, queryScope);

            if (user == null)
            {
                return Results.NotFound(new { error = "user_not_found", userId });
            }

            return Results.Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user {UserId}", userId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateUserStatus(
        Guid userId,
        [FromBody] UpdateUserStatusRequest request,
        IBackofficeUserService backofficeUserService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyBackofficeUserEndpoints");

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context para roles no-SUPER_ADMIN
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            // Resolver scope
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var updateRequest = new UpdateBackofficeUserRequest(Status: request.Status);
            var response = await backofficeUserService.UpdateUserAsync(userId, updateRequest, currentUserId, queryScope);

            logger.LogInformation("User status updated: {UserId} to {Status} by {CurrentUserId}",
                userId, request.Status, currentUserId);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("User status update failed: {Error}", ex.Message);
            
            if (ex.Message.Contains("not found"))
                return Results.NotFound(new { error = "user_not_found", userId });
                
            return Results.Problem(
                title: "Update Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user status {UserId}", userId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateUserRole(
        Guid userId,
        [FromBody] UpdateUserRoleRequest request,
        IBackofficeUserService backofficeUserService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyBackofficeUserEndpoints");

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Solo SUPER_ADMIN puede cambiar roles (ya validado por policy)
            // Validar permisos adicionales
            var userPermissionValidation = AuthorizationHelper.ValidateUserOperationPermissions(
                currentRole, newRole: request.Role);
            if (userPermissionValidation != null) return userPermissionValidation;

            // Para SUPER_ADMIN, el scope puede ser global (null) o específico según el target user
            // Si el target user está en un brand específico, usar ese scope
            var queryScope = currentRole == BackofficeUserRole.SUPER_ADMIN ? null : 
                AuthorizationHelper.GetQueryScope(currentRole, currentUserBrandId, brandContext);

            var updateRequest = new UpdateBackofficeUserRequest(Role: request.Role);
            var response = await backofficeUserService.UpdateUserAsync(userId, updateRequest, currentUserId, queryScope);

            logger.LogInformation("User role updated: {UserId} to {Role} by {CurrentUserId}",
                userId, request.Role, currentUserId);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("User role update failed: {Error}", ex.Message);
            
            if (ex.Message.Contains("not found"))
                return Results.NotFound(new { error = "user_not_found", userId });
                
            return Results.Problem(
                title: "Update Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user role {UserId}", userId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteBackofficeUser(
        Guid userId,
        IBackofficeUserService backofficeUserService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyBackofficeUserEndpoints");

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context para roles no-SUPER_ADMIN
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            // Resolver scope
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var deleted = await backofficeUserService.DeleteUserAsync(userId, currentUserId, queryScope);

            if (!deleted)
            {
                return Results.NotFound(new { error = "user_not_found", userId });
            }

            logger.LogInformation("User deleted: {UserId} by {CurrentUserId}", userId, currentUserId);

            return Results.Ok(new { success = true, message = "User deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("User deletion failed: {Error}", ex.Message);
            
            if (ex.Message.Contains("subordinates"))
                return Results.Conflict(new { error = "has_subordinates", message = ex.Message });
            
            return Results.Problem(
                title: "Deletion Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user {UserId}", userId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }
}