using Casino.Api.Utils;
using Casino.Application.DTOs.Admin;
using Casino.Application.DTOs.Player;
using Casino.Application.DTOs.UnifiedUser;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

/// <summary>
/// SONNET: Endpoints unificados para gestión completa de usuarios (CRUD)
/// Consolida BackofficeUsers y Players en una única interfaz /users
/// Elimina duplicación con BrandOnlyBackofficeUserEndpoints
/// </summary>
public static class UnifiedUserEndpoints
{
    public static void MapUnifiedUserEndpoints(this IEndpointRouteBuilder app)
    {
        // === UNIFIED USER MANAGEMENT (CRUD COMPLETO) ===
        
        // SONNET: POST /users - Crear usuario de cualquier tipo
        app.MapPost("/users", CreateUser)
            .RequireAuthorization("AnyBackofficeUser")
            .WithName("CreateUserUnified")
            .WithSummary("Create user (backoffice or player)")
            .WithDescription("Creates a new user. Type determined by Role field. SUPER_ADMIN→any, BRAND_ADMIN→BRAND_ADMIN|CASHIER|PLAYER, CASHIER→CASHIER|PLAYER")
            .Produces<UnifiedUserResponse>(201)
            .Produces(400)
            .Produces(403)
            .Produces(409);

        // SONNET: GET /users - Listar todos los usuarios (backoffice + players)
        app.MapGet("/users", GetAllUsers)
            .RequireAuthorization("AnyBackofficeUser")
            .WithName("GetAllUsersUnified")
            .WithSummary("Get all users (backoffice + players) unified")
            .WithDescription("Returns all users (backoffice and players) that the current user can see based on their role and scope. SUPER_ADMIN can use ?globalScope=true.")
            .Produces<QueryUnifiedUsersResponse>()
            .Produces(400)
            .Produces(403);

        // SONNET: GET /users/{id} - Ver detalles de usuario
        app.MapGet("/users/{userId:guid}", GetUserById)
            .RequireAuthorization("AnyBackofficeUser")
            .WithName("GetUserByIdUnified")
            .WithSummary("Get user by ID (searches both backoffice and players)")
            .WithDescription("Searches for a user by ID in both BackofficeUsers and Players tables")
            .Produces<UnifiedUserResponse>()
            .Produces(404)
            .Produces(403);

        // SONNET: PATCH /users/{id} - Editar usuario
        app.MapPatch("/users/{userId:guid}", UpdateUser)
            .RequireAuthorization("AnyBackofficeUser")
            .WithName("UpdateUserUnified")
            .WithSummary("Update user (backoffice or player)")
            .WithDescription("Updates user fields (role, status, commission, etc.) with hierarchical validation")
            .Produces<UnifiedUserResponse>()
            .Produces(404)
            .Produces(403);

        // SONNET: DELETE /users/{id} - Desactivar/eliminar usuario
        app.MapDelete("/users/{userId:guid}", DeleteUser)
            .RequireAuthorization("AdminOrSuperAdmin")
            .WithName("DeleteUserUnified")
            .WithSummary("Delete user (backoffice or player)")
            .WithDescription("Deletes a user with hierarchical validation")
            .Produces(200)
            .Produces(404)
            .Produces(403)
            .Produces(409);

        // SONNET: GET /users/search - Buscar por username
        app.MapGet("/users/search", SearchUserByUsername)
            .RequireAuthorization("AnyBackofficeUser")
            .WithName("SearchUserByUsernameUnified")
            .WithSummary("Search user by username (searches both tables)")
            .WithDescription("Searches for a user by username in both BackofficeUsers and Players tables")
            .Produces<UnifiedUserResponse>()
            .Produces(404)
            .Produces(403);
    }

    /// <summary>
    /// SONNET: Crear usuario unificado (backoffice o player según request)
    /// </summary>
    private static async Task<IResult> CreateUser(
        [FromBody] CreateUnifiedUserRequest request,
        [FromServices] IUnifiedUserService unifiedUserService,
        [FromServices] IBackofficeUserService backofficeUserService,
        [FromServices] IPlayerService playerService,
        [FromServices] IValidator<CreateUnifiedUserRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        // Validar request
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // SONNET: Validar jerarquía de permisos
            if (request.Role.HasValue)
            {
                var permissionValidation = AuthorizationHelper.ValidateUserOperationPermissions(
                    currentRole, newRole: request.Role.Value);
                if (permissionValidation != null) return permissionValidation;
            }

            // SONNET: Determinar si es usuario backoffice o player
            bool isPlayer = !request.Role.HasValue || request.Role.Value == BackofficeUserRole.PLAYER;

            if (isPlayer)
            {
                // Crear player
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;

                var effectiveBrandId = AuthorizationHelper.GetEffectiveBrandId(
                    currentRole, currentUserBrandId, brandContext);

                var playerRequest = new CreatePlayerRequest(
                    request.Username,
                    request.Email,
                    request.ExternalId,
                    0, // Initial balance
                    PlayerStatus.ACTIVE,
                    request.Password);

                var playerResponse = await playerService.CreatePlayerAsync(
                    playerRequest, currentUserId, effectiveBrandId, currentRole);

                logger.LogInformation("Player created via unified endpoint: {PlayerId} by {UserId}",
                    playerResponse.Id, currentUserId);

                // Mapear a UnifiedUserResponse
                var unifiedResponse = new UnifiedUserResponse(
                    playerResponse.Id,
                    "PLAYER",
                    playerResponse.Username,
                    playerResponse.Email,
                    null, // Role
                    playerResponse.Status.ToString(),
                    playerResponse.BrandId,
                    playerResponse.BrandName,
                    null, // ParentCashierId
                    null, // ParentCashierUsername
                    0, // CommissionPercent
                    0, // SubordinatesCount
                    playerResponse.Balance,
                    playerResponse.CreatedAt,
                    null, // LastLoginAt
                    currentUserId, // CreatedByUserId
                    null, // CreatedByUsername (no disponible aquí)
                    currentRole.ToString()); // CreatedByRole

                return Results.Created($"/api/v1/admin/users/{unifiedResponse.Id}", unifiedResponse);
            }
            else
            {
                // Crear backoffice user
                Guid? effectiveBrandId = null;

                if (request.Role.Value != BackofficeUserRole.SUPER_ADMIN)
                {
                    var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                        currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                    if (brandValidation != null) return brandValidation;

                    effectiveBrandId = AuthorizationHelper.GetEffectiveBrandId(
                        currentRole, currentUserBrandId, brandContext);
                }

                var backofficeRequest = new CreateBackofficeUserRequest(
                    request.Username,
                    request.Password ?? throw new InvalidOperationException("Password required for backoffice users"),
                    request.Role.Value,
                    request.ParentCashierId,
                    request.CommissionPercent);

                var backofficeResponse = await backofficeUserService.CreateUserAsync(
                    backofficeRequest, currentUserId, effectiveBrandId);

                logger.LogInformation("Backoffice user created via unified endpoint: {UserId} by {CreatedBy}",
                    backofficeResponse.Id, currentUserId);

                // Mapear a UnifiedUserResponse
                var unifiedResponse = new UnifiedUserResponse(
                    backofficeResponse.Id,
                    "BACKOFFICE",
                    backofficeResponse.Username,
                    null, // Email
                    backofficeResponse.Role.ToString(),
                    backofficeResponse.Status.ToString(),
                    backofficeResponse.BrandId,
                    backofficeResponse.BrandName,
                    backofficeResponse.ParentCashierId,
                    backofficeResponse.ParentCashierUsername,
                    backofficeResponse.CommissionPercent,
                    backofficeResponse.SubordinatesCount,
                    0, // Balance
                    backofficeResponse.CreatedAt,
                    backofficeResponse.LastLoginAt,
                    currentUserId, // CreatedByUserId
                    null, // CreatedByUsername (no disponible aquí)
                    currentRole.ToString()); // CreatedByRole

                return Results.Created($"/api/v1/admin/users/{unifiedResponse.Id}", unifiedResponse);
            }
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("User creation failed: {Error}", ex.Message);

            if (ex.Message.Contains("already exists"))
                return Results.Conflict(new { error = "username_exists", message = ex.Message });

            return Results.Problem(title: "User Creation Failed", detail: ex.Message, statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating user");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    /// <summary>
    /// SONNET: Actualizar usuario unificado
    /// </summary>
    private static async Task<IResult> UpdateUser(
        Guid userId,
        [FromBody] UpdateUnifiedUserRequest request,
        [FromServices] IUnifiedUserService unifiedUserService,
        [FromServices] IBackofficeUserService backofficeUserService,
        [FromServices] IPlayerService playerService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Primero buscar el usuario para determinar su tipo
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var existingUser = await unifiedUserService.GetUserByIdAsync(userId, queryScope, currentUserId, currentRole);

            if (existingUser == null)
            {
                return Results.NotFound(new { error = "user_not_found", userId });
            }

            if (existingUser.UserType == "PLAYER")
            {
                // Actualizar player
                var playerUpdateRequest = new UpdatePlayerRequest(
                    request.Username,
                    request.Email,
                    !string.IsNullOrEmpty(request.Status) ? Enum.Parse<PlayerStatus>(request.Status) : null);

                var playerResponse = await playerService.UpdatePlayerAsync(
                    userId, playerUpdateRequest, currentUserId, queryScope);

                logger.LogInformation("Player updated via unified endpoint: {PlayerId} by {UserId}",
                    userId, currentUserId);

                var unifiedResponse = new UnifiedUserResponse(
                    playerResponse.Id,
                    "PLAYER",
                    playerResponse.Username,
                    playerResponse.Email,
                    null,
                    playerResponse.Status.ToString(),
                    playerResponse.BrandId,
                    playerResponse.BrandName,
                    null, null, 0, 0,
                    playerResponse.Balance,
                    playerResponse.CreatedAt,
                    null,
                    existingUser.CreatedByUserId,
                    existingUser.CreatedByUsername,
                    existingUser.CreatedByRole);

                return Results.Ok(unifiedResponse);
            }
            else
            {
                // Actualizar backoffice user
                var backofficeUpdateRequest = new UpdateBackofficeUserRequest(
                    request.Username,
                    request.Password,
                    !string.IsNullOrEmpty(request.Role) ? Enum.Parse<BackofficeUserRole>(request.Role) : null,
                    !string.IsNullOrEmpty(request.Status) ? Enum.Parse<BackofficeUserStatus>(request.Status) : null,
                    request.CommissionPercent);

                var backofficeResponse = await backofficeUserService.UpdateUserAsync(
                    userId, backofficeUpdateRequest, currentUserId, queryScope);

                logger.LogInformation("Backoffice user updated via unified endpoint: {UserId} by {UpdatedBy}",
                    userId, currentUserId);

                var unifiedResponse = new UnifiedUserResponse(
                    backofficeResponse.Id,
                    "BACKOFFICE",
                    backofficeResponse.Username,
                    null,
                    backofficeResponse.Role.ToString(),
                    backofficeResponse.Status.ToString(),
                    backofficeResponse.BrandId,
                    backofficeResponse.BrandName,
                    backofficeResponse.ParentCashierId,
                    backofficeResponse.ParentCashierUsername,
                    backofficeResponse.CommissionPercent,
                    backofficeResponse.SubordinatesCount,
                    0,
                    backofficeResponse.CreatedAt,
                    backofficeResponse.LastLoginAt,
                    existingUser.CreatedByUserId,
                    existingUser.CreatedByUsername,
                    existingUser.CreatedByRole);

                return Results.Ok(unifiedResponse);
            }
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("User update failed: {Error}", ex.Message);

            if (ex.Message.Contains("not found"))
                return Results.NotFound(new { error = "user_not_found", userId });

            return Results.Problem(title: "Update Failed", detail: ex.Message, statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user {UserId}", userId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    /// <summary>
    /// SONNET: Eliminar usuario unificado
    /// </summary>
    private static async Task<IResult> DeleteUser(
        Guid userId,
        [FromServices] IUnifiedUserService unifiedUserService,
        [FromServices] IBackofficeUserService backofficeUserService,
        [FromServices] IPlayerService playerService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var existingUser = await unifiedUserService.GetUserByIdAsync(userId, queryScope, currentUserId, currentRole);

            if (existingUser == null)
            {
                return Results.NotFound(new { error = "user_not_found", userId });
            }

            bool deleted;
            if (existingUser.UserType == "PLAYER")
            {
                deleted = await playerService.DeletePlayerAsync(userId, currentUserId, queryScope);
                logger.LogInformation("Player deleted via unified endpoint: {PlayerId} by {UserId}",
                    userId, currentUserId);
            }
            else
            {
                deleted = await backofficeUserService.DeleteUserAsync(userId, currentUserId, queryScope);
                logger.LogInformation("Backoffice user deleted via unified endpoint: {UserId} by {DeletedBy}",
                    userId, currentUserId);
            }

            if (!deleted)
            {
                return Results.NotFound(new { error = "user_not_found", userId });
            }

            return Results.Ok(new { success = true, message = "User deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("User deletion failed: {Error}", ex.Message);

            if (ex.Message.Contains("subordinates") || ex.Message.Contains("activity"))
                return Results.Conflict(new { error = "cannot_delete", message = ex.Message });

            return Results.Problem(title: "Deletion Failed", detail: ex.Message, statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user {UserId}", userId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    /// <summary>
    /// Obtiene todos los usuarios (backoffice + players) según el scope del usuario actual
    /// SONNET: Restaura la funcionalidad original de GET /users
    /// </summary>
    private static async Task<IResult> GetAllUsers(
        [FromServices] IUnifiedUserService unifiedUserService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger,
        [FromQuery] string? username = null,
        [FromQuery] string? userType = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        [FromQuery] bool globalScope = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // SONNET: Crear request manualmente para evitar problemas con [AsParameters]
            var request = new QueryUnifiedUsersRequest
            {
                Username = username,
                UserType = userType,
                Role = role,
                Status = status,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                GlobalScope = globalScope,
                Page = page,
                PageSize = pageSize
            };

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

            var response = await unifiedUserService.GetAllUsersAsync(
                request, queryScope, currentUserId, currentRole);

            logger.LogInformation("Unified users listed - Role: {Role}, Scope: {Scope}, Total: {Total}, UserType: {UserType}", 
                currentRole, response.AppliedScope, response.TotalCount, request.UserType ?? "ALL");

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting unified users");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    /// <summary>
    /// Busca un usuario por ID en ambas tablas (BackofficeUsers y Players)
    /// </summary>
    private static async Task<IResult> GetUserById(
        Guid userId,
        [FromServices] IUnifiedUserService unifiedUserService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
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

            var user = await unifiedUserService.GetUserByIdAsync(userId, queryScope, currentUserId, currentRole);

            if (user == null)
            {
                return Results.NotFound(new { error = "user_not_found", userId });
            }

            logger.LogInformation("User found - ID: {UserId}, Type: {UserType}, Username: {Username}", 
                userId, user.UserType, user.Username);

            return Results.Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    /// <summary>
    /// Busca un usuario por username en ambas tablas
    /// </summary>
    private static async Task<IResult> SearchUserByUsername(
        [FromQuery] string username,
        [FromServices] IUnifiedUserService unifiedUserService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Results.BadRequest(new { error = "username_required", message = "Username parameter is required" });
        }

        try
        {
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
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

            var user = await unifiedUserService.FindUserByUsernameAsync(username, queryScope, currentUserId, currentRole);

            if (user == null)
            {
                return Results.NotFound(new { error = "user_not_found", username });
            }

            logger.LogInformation("User found by username - Username: {Username}, Type: {UserType}, ID: {UserId}", 
                username, user.UserType, user.Id);

            return Results.Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching user by username {Username}", username);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }
}