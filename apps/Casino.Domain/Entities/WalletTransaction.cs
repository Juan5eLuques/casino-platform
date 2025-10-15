using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

/// <summary>
/// Tabla simple para registrar transacciones/transferencias entre wallets
/// SONNET: Incluye IdempotencyKey + BrandId + campos de actor para seguridad
/// SONNET: Incluye PreviousBalance/NewBalance para auditoría completa
/// </summary>
public class WalletTransaction
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Brand donde ocurre la transacción (para scope de autorización)
    /// </summary>
    public Guid BrandId { get; set; }
    
    /// <summary>
    /// Usuario origen (null para MINT)
    /// </summary>
    public Guid? FromUserId { get; set; }
    
    /// <summary>
    /// Tipo de usuario origen: BACKOFFICE o PLAYER
    /// </summary>
    public string? FromUserType { get; set; }
    
    /// <summary>
    /// Usuario destino
    /// </summary>
    public Guid ToUserId { get; set; }
    
    /// <summary>
    /// Tipo de usuario destino: BACKOFFICE o PLAYER
    /// </summary>
    public string ToUserType { get; set; } = string.Empty;
    
    /// <summary>
    /// Monto transferido (siempre positivo)
    /// </summary>
    public decimal Amount { get; set; }
    
    // SONNET: Campos de auditoría para balances antes/después de la transacción
    
    /// <summary>
    /// Balance del usuario origen ANTES de la transacción (null para MINT)
    /// </summary>
    public decimal? PreviousBalanceFrom { get; set; }
    
    /// <summary>
    /// Balance del usuario origen DESPUÉS de la transacción (null para MINT)
    /// </summary>
    public decimal? NewBalanceFrom { get; set; }
    
    /// <summary>
    /// Balance del usuario destino ANTES de la transacción
    /// </summary>
    public decimal? PreviousBalanceTo { get; set; }
    
    /// <summary>
    /// Balance del usuario destino DESPUÉS de la transacción
    /// </summary>
    public decimal? NewBalanceTo { get; set; }
    
    /// <summary>
    /// Descripción de la transacción
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Tipo de transacción para categorización estándar de casino
    /// </summary>
    public TransactionType? TransactionType { get; set; }
    
    /// <summary>
    /// Usuario que ejecutó la transacción
    /// </summary>
    public Guid CreatedByUserId { get; set; }
    
    /// <summary>
    /// Rol del usuario que ejecutó la transacción
    /// </summary>
    public string CreatedByRole { get; set; } = string.Empty;
    
    /// <summary>
    /// SONNET: Clave de idempotencia única para evitar transacciones duplicadas
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Fecha de la transacción
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public BackofficeUser CreatedByUser { get; set; } = null!;
}