using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

/// <summary>
/// Tabla simple para registrar transacciones/transferencias entre wallets
/// SONNET: Incluye IdempotencyKey + BrandId + campos de actor para seguridad
/// SONNET: Incluye PreviousBalance/NewBalance para auditor�a completa
/// </summary>
public class WalletTransaction
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Brand donde ocurre la transacci�n (para scope de autorizaci�n)
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
    
    // SONNET: Campos de auditor�a para balances antes/despu�s de la transacci�n
    
    /// <summary>
    /// Balance del usuario origen ANTES de la transacci�n (null para MINT)
    /// </summary>
    public decimal? PreviousBalanceFrom { get; set; }
    
    /// <summary>
    /// Balance del usuario origen DESPU�S de la transacci�n (null para MINT)
    /// </summary>
    public decimal? NewBalanceFrom { get; set; }
    
    /// <summary>
    /// Balance del usuario destino ANTES de la transacci�n
    /// </summary>
    public decimal? PreviousBalanceTo { get; set; }
    
    /// <summary>
    /// Balance del usuario destino DESPU�S de la transacci�n
    /// </summary>
    public decimal? NewBalanceTo { get; set; }
    
    /// <summary>
    /// Descripci�n de la transacci�n
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Tipo de transacci�n para categorizaci�n est�ndar de casino
    /// </summary>
    public TransactionType? TransactionType { get; set; }
    
    /// <summary>
    /// Usuario que ejecut� la transacci�n
    /// </summary>
    public Guid CreatedByUserId { get; set; }
    
    /// <summary>
    /// Rol del usuario que ejecut� la transacci�n
    /// </summary>
    public string CreatedByRole { get; set; } = string.Empty;
    
    /// <summary>
    /// SONNET: Clave de idempotencia �nica para evitar transacciones duplicadas
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Fecha de la transacci�n
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public BackofficeUser CreatedByUser { get; set; } = null!;
}