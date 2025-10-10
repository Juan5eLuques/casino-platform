using Casino.Application.DTOs.UnifiedUser;
using Casino.Domain.Enums;

namespace Casino.Application.Services;

/// <summary>
/// Servicio unificado para gestión de todos los tipos de usuarios (BackofficeUsers + Players)
/// Permite operaciones sobre cualquier tipo de usuario sin diferenciar el tipo
/// </summary>
public interface IUnifiedUserService
{
    /// <summary>
    /// Obtener todos los usuarios (backoffice + players) según scope del usuario actual
    /// </summary>
    Task<QueryUnifiedUsersResponse> GetAllUsersAsync(QueryUnifiedUsersRequest request, Guid? brandScope, Guid currentUserId, BackofficeUserRole currentRole);

    /// <summary>
    /// Obtener un usuario específico por ID (busca en ambas tablas)
    /// </summary>
    Task<UnifiedUserResponse?> GetUserByIdAsync(Guid userId, Guid? brandScope, Guid currentUserId, BackofficeUserRole currentRole);

    /// <summary>
    /// Buscar usuario por username (busca en ambas tablas)
    /// </summary>
    Task<UnifiedUserResponse?> FindUserByUsernameAsync(string username, Guid? brandScope, Guid currentUserId, BackofficeUserRole currentRole);
}