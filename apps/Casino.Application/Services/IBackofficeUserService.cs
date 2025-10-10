using Casino.Application.DTOs.Admin;
using Casino.Domain.Enums;

namespace Casino.Application.Services;

public interface IBackofficeUserService
{
    /// <summary>
    /// Crea un nuevo usuario de backoffice. El brand se resuelve autom�ticamente seg�n el rol.
    /// effectiveBrandId puede ser null para SUPER_ADMIN, requerido para BRAND_ADMIN/CASHIER.
    /// </summary>
    Task<GetBackofficeUserResponse> CreateUserAsync(CreateBackofficeUserRequest request, Guid currentUserId, Guid? effectiveBrandId);
    
    /// <summary>
    /// Obtiene usuarios con paginaci�n y filtros. Scope por brand autom�tico.
    /// Para CASHIER: incluir currentUserId y currentUserRole para filtrar por jerarqu�a.
    /// </summary>
    Task<QueryBackofficeUsersResponse> GetUsersAsync(QueryBackofficeUsersRequest request, Guid? brandScope = null, Guid? currentUserId = null, BackofficeUserRole? currentUserRole = null);
    
    /// <summary>
    /// Obtiene un usuario espec�fico. Scope por brand autom�tico.
    /// </summary>
    Task<GetBackofficeUserResponse?> GetUserAsync(Guid userId, Guid? brandScope = null);
    
    /// <summary>
    /// Obtiene la jerarqu�a de un usuario (para cashiers). Scope por brand autom�tico.
    /// </summary>
    Task<GetBackofficeUserHierarchyResponse?> GetUserHierarchyAsync(Guid userId, Guid? brandScope = null);
    
    /// <summary>
    /// Actualiza un usuario existente. Scope por brand autom�tico.
    /// </summary>
    Task<GetBackofficeUserResponse> UpdateUserAsync(Guid userId, UpdateBackofficeUserRequest request, Guid currentUserId, Guid? brandScope = null);
    
    /// <summary>
    /// Elimina un usuario. Scope por brand autom�tico.
    /// </summary>
    Task<bool> DeleteUserAsync(Guid userId, Guid currentUserId, Guid? brandScope = null);
}