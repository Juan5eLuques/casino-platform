using Casino.Application.DTOs.Admin;
using Casino.Domain.Enums;

namespace Casino.Application.Services;

/// <summary>
/// Servicio para gestionar el árbol genealógico de usuarios
/// </summary>
public interface IUserTreeService
{
    /// <summary>
    /// Obtiene el árbol genealógico de un usuario (usuarios creados por él)
    /// </summary>
    Task<GetUserTreeResponse?> GetUserTreeAsync(
        Guid userId, 
        GetUserTreeRequest request,
        Guid? brandScope,
        Guid currentUserId,
        BackofficeUserRole currentRole);
}
