using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Admin;

// Request DTOs - SIN BrandId (se resuelve automáticamente por Host)
// SONNET: CommissionRate renombrado a CommissionPercent para consistencia
public record CreateBackofficeUserRequest(
    string Username,
    string Password,
    BackofficeUserRole Role,
    Guid? ParentCashierId = null, // ID del cashier padre (solo para CASHIER)
    decimal CommissionRate = 0); // NOTA: Mantener nombre para compatibilidad con API, mapea a CommissionPercent internamente

public record UpdateBackofficeUserRequest(
    string? Username = null,
    string? Password = null,
    BackofficeUserRole? Role = null,
    BackofficeUserStatus? Status = null,
    decimal? CommissionRate = null); // NOTA: Mantener nombre para compatibilidad con API

public record QueryBackofficeUsersRequest(
    string? Username = null,
    BackofficeUserRole? Role = null,
    BackofficeUserStatus? Status = null,
    Guid? ParentCashierId = null, // Filtrar por cashier padre
    bool IncludeSubordinates = false, // Incluir subordinados en la respuesta
    bool GlobalScope = false, // Solo para SUPER_ADMIN: ver todos los brands
    int Page = 1,
    int PageSize = 20);

// Response DTOs
// SONNET: CommissionPercent en responses para consistencia con modelo de dominio
public record GetBackofficeUserResponse(
    Guid Id,
    string Username,
    BackofficeUserRole Role,
    BackofficeUserStatus Status,
    Guid? BrandId, // Null para SUPER_ADMIN, BrandId para otros roles
    string? BrandName, // Null para SUPER_ADMIN, BrandName para otros roles
    Guid? ParentCashierId,
    string? ParentCashierUsername,
    decimal CommissionPercent, // SONNET: Renombrado de CommissionRate
    int SubordinatesCount,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record GetBackofficeUserHierarchyResponse(
    Guid Id,
    string Username,
    BackofficeUserRole Role,
    BackofficeUserStatus Status,
    Guid? ParentCashierId,
    decimal CommissionPercent, // SONNET: Renombrado de CommissionRate
    DateTime CreatedAt,
    IEnumerable<GetBackofficeUserHierarchyResponse> Subordinates);

public record QueryBackofficeUsersResponse(
    IEnumerable<GetBackofficeUserResponse> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);