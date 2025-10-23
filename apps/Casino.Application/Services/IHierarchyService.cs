using Casino.Domain.Entities;

namespace Casino.Application.Services;

/// <summary>
/// Servicio para manejar operaciones de jerarquía multinivel de usuarios
/// </summary>
public interface IHierarchyService
{
    /// <summary>
    /// Obtiene todos los usuarios descendientes de un usuario (árbol hacia abajo)
    /// Incluye todos los niveles: hijos, nietos, bisnietos, etc.
    /// </summary>
    /// <param name="userId">ID del usuario raíz</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de descendientes ordenados por nivel jerárquico</returns>
    Task<IEnumerable<BackofficeUser>> GetDescendantsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene la cadena de ancestros hasta SUPER_ADMIN
    /// Retorna: [Usuario Actual ? Padre ? Abuelo ? ... ? SUPER_ADMIN]
    /// </summary>
    /// <param name="userId">ID del usuario inicial</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de ancestros ordenados del más cercano al más lejano</returns>
    Task<IEnumerable<BackofficeUser>> GetAncestorsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Valida si un usuario puede operar sobre otro basado en jerarquía
    /// Regla: Solo puedes operar sobre tus descendientes
    /// Excepción: SUPER_ADMIN puede operar sobre cualquiera
    /// </summary>
    /// <param name="actorId">Usuario que intenta realizar la operación</param>
    /// <param name="targetId">Usuario sobre el que se quiere operar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si la operación está permitida</returns>
    Task<bool> CanOperateOnAsync(
        Guid actorId, 
        Guid targetId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calcula y actualiza el hierarchy_path de un usuario
    /// Se llama automáticamente cuando se cambia el parent_admin_id
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>El nuevo hierarchy_path calculado</returns>
    Task<string> CalculateHierarchyPathAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene todos los usuarios de un nivel jerárquico específico
    /// </summary>
    /// <param name="level">Nivel (0=SUPER_ADMIN, 1=BRAND_ADMIN, etc.)</param>
    /// <param name="brandId">ID del brand (opcional para filtrar)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de usuarios en ese nivel</returns>
    Task<IEnumerable<BackofficeUser>> GetUsersByLevelAsync(
        int level, 
        Guid? brandId = null, 
        CancellationToken cancellationToken = default);
}
