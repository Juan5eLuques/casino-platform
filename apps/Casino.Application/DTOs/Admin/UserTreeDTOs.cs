namespace Casino.Application.DTOs.Admin;

/// <summary>
/// Response para el árbol genealógico de un usuario
/// </summary>
public record GetUserTreeResponse(
    Guid RootUserId,
    string RootUsername,
    string RootUserType,
    string? Role,
    UserTreeNode Tree
);

/// <summary>
/// Nodo del árbol con información del usuario y sus hijos directos
/// </summary>
public record UserTreeNode(
    Guid Id,
    string Username,
    string UserType, // "BACKOFFICE" o "PLAYER"
    string? Role, // Rol si es backoffice
    string Status,
    DateTime CreatedAt,
    decimal Balance, // Balance actual del usuario (WalletBalance)
    decimal? CommissionPercent, // Comisión (solo para CASHIER con ParentCashierId)
    bool HasChildren, // TRUE si este usuario tiene hijos (creó otros usuarios)
    int DirectChildrenCount, // Cantidad de hijos directos
    IEnumerable<UserTreeNode>? Children // Puede ser null si no se cargaron los hijos aún
);

/// <summary>
/// Request para obtener el árbol con opciones de expansión
/// </summary>
public record GetUserTreeRequest(
    int MaxDepth = 1, // Profundidad máxima del árbol (1 = solo hijos directos, 2 = nietos, etc.)
    bool IncludeInactive = false // Incluir usuarios inactivos
);
