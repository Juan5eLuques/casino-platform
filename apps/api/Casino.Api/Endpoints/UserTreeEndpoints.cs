using Casino.Api.Utils;
using Casino.Application.DTOs.Admin;
using Casino.Application.Services;
using Casino.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

/// <summary>
/// Endpoints para visualizar el árbol genealógico de usuarios
/// Muestra qué usuarios fueron creados por cada usuario de forma jerárquica
/// </summary>
public static class UserTreeEndpoints
{
    public static void MapUserTreeEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/v1/admin/tree/{userId} - Obtener árbol genealógico
        app.MapGet("/tree/{userId:guid}", GetUserTree)
            .RequireAuthorization("AnyBackofficeUser")
            .WithName("GetUserTree")
            .WithSummary("Get user genealogy tree")
            .WithDescription("Returns the genealogy tree showing all users created by the specified user (children, grandchildren, etc.)")
            .Produces<GetUserTreeResponse>()
            .Produces(404)
            .Produces(403);
    }

    /// <summary>
    /// Obtiene el árbol genealógico de un usuario
    /// </summary>
    private static async Task<IResult> GetUserTree(
        Guid userId,
        [FromServices] IUserTreeService userTreeService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger,
        [FromQuery] int maxDepth = 1,
        [FromQuery] bool includeInactive = false)
    {
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

            // Validar maxDepth razonable
            if (maxDepth < 1 || maxDepth > 10)
            {
                return Results.BadRequest(new
                {
                    error = "invalid_max_depth",
                    message = "MaxDepth must be between 1 and 10"
                });
            }

            // Resolver scope
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var request = new GetUserTreeRequest(maxDepth, includeInactive);

            var treeResponse = await userTreeService.GetUserTreeAsync(
                userId, request, queryScope, currentUserId, currentRole);

            if (treeResponse == null)
            {
                return Results.NotFound(new
                {
                    error = "user_not_found",
                    userId,
                    message = "User not found or access denied"
                });
            }

            logger.LogInformation(
                "User tree retrieved: UserId={UserId}, UserType={UserType}, MaxDepth={MaxDepth}, TotalChildren={TotalChildren}",
                userId, treeResponse.RootUserType, maxDepth, treeResponse.Tree.DirectChildrenCount);

            return Results.Ok(treeResponse);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Access denied"))
        {
            logger.LogWarning("Access denied getting user tree for userId: {UserId} - {Error}",
                userId, ex.Message);
            return Results.Problem(
                title: "Access Denied",
                detail: ex.Message,
                statusCode: 403);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user tree for userId: {UserId}", userId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while retrieving the user tree",
                statusCode: 500);
        }
    }
}
