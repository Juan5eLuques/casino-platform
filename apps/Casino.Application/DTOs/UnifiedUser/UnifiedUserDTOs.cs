using Casino.Domain.Enums;

namespace Casino.Application.DTOs.UnifiedUser;

/// <summary>
/// Request para consultar todos los usuarios unificados
/// </summary>
public record QueryUnifiedUsersRequest
{
    public string? Username { get; init; }
    public string? UserType { get; init; } // "BACKOFFICE", "PLAYER", o null para ambos
    public string? Role { get; init; } // Solo para usuarios backoffice
    public string? Status { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public bool GlobalScope { get; init; } = false; // Solo SUPER_ADMIN
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// SONNET: Request para crear usuario unificado (backoffice o player)
/// </summary>
public record CreateUnifiedUserRequest(
    string Username,
    string? Password, // Required para backoffice, opcional para player
    string? Email, // Para players
    string? ExternalId, // Para players
    BackofficeUserRole? Role, // Null = PLAYER, otro valor = BackofficeUser
    Guid? ParentCashierId = null, // Solo para CASHIER
    decimal CommissionPercent = 0 // Solo para CASHIER (0-100)
);

/// <summary>
/// SONNET: Request para actualizar usuario unificado
/// </summary>
public record UpdateUnifiedUserRequest(
    string? Username = null,
    string? Password = null, // Solo para backoffice
    string? Email = null, // Solo para player
    string? Role = null, // Para cambiar rol (solo SUPER_ADMIN)
    string? Status = null,
    decimal? CommissionPercent = null // Solo para CASHIER
);

/// <summary>
/// Respuesta unificada de un usuario (puede ser BackofficeUser o Player)
/// SONNET: Actualizado para incluir CommissionPercent en lugar de CommissionRate
/// SONNET: Incluye información del usuario que creó este usuario
/// </summary>
public record UnifiedUserResponse(
    Guid Id,
    string UserType, // "BACKOFFICE" o "PLAYER"
    string Username,
    string? Email,
    string? Role, // Solo para usuarios backoffice
    string Status,
    Guid? BrandId,
    string? BrandName,
    Guid? ParentCashierId,
    string? ParentCashierUsername,
    decimal CommissionPercent, // SONNET: Renombrado de CommissionRate
    int SubordinatesCount,
    decimal WalletBalance,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    // SONNET: Información del creador
    Guid? CreatedByUserId,
    string? CreatedByUsername,
    string? CreatedByRole
);

/// <summary>
/// Respuesta paginada de usuarios unificados
/// </summary>
public record QueryUnifiedUsersResponse(
    List<UnifiedUserResponse> Data,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    string? AppliedScope // Para debugging: "global", "brand:guid", etc.
);