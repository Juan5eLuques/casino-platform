namespace Casino.Domain.Enums;

/// <summary>
/// Tipo de transacción para categorización estándar de casino en WalletTransactions
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Emisión de fondos por SUPER_ADMIN
    /// </summary>
    MINT,
    
    /// <summary>
    /// Transferencia entre usuarios del sistema
    /// </summary>
    TRANSFER,
    
    /// <summary>
    /// Apuesta de jugador (débito)
    /// </summary>
    BET,
    
    /// <summary>
    /// Ganancia de jugador (crédito)
    /// </summary>
    WIN,
    
    /// <summary>
    /// Reversión de transacción
    /// </summary>
    ROLLBACK,
    
    /// <summary>
    /// Depósito externo (futuro)
    /// </summary>
    DEPOSIT,
    
    /// <summary>
    /// Retiro externo (futuro)
    /// </summary>
    WITHDRAWAL,
    
    /// <summary>
    /// Bonificación o promoción (futuro)
    /// </summary>
    BONUS,
    
    /// <summary>
    /// Ajuste manual (futuro)
    /// </summary>
    ADJUSTMENT
}
