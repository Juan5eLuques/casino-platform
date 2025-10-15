namespace Casino.Domain.Enums;

/// <summary>
/// Tipo de transacci�n para categorizaci�n est�ndar de casino en WalletTransactions
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Emisi�n de fondos por SUPER_ADMIN
    /// </summary>
    MINT,
    
    /// <summary>
    /// Transferencia entre usuarios del sistema
    /// </summary>
    TRANSFER,
    
    /// <summary>
    /// Apuesta de jugador (d�bito)
    /// </summary>
    BET,
    
    /// <summary>
    /// Ganancia de jugador (cr�dito)
    /// </summary>
    WIN,
    
    /// <summary>
    /// Reversi�n de transacci�n
    /// </summary>
    ROLLBACK,
    
    /// <summary>
    /// Dep�sito externo (futuro)
    /// </summary>
    DEPOSIT,
    
    /// <summary>
    /// Retiro externo (futuro)
    /// </summary>
    WITHDRAWAL,
    
    /// <summary>
    /// Bonificaci�n o promoci�n (futuro)
    /// </summary>
    BONUS,
    
    /// <summary>
    /// Ajuste manual (futuro)
    /// </summary>
    ADJUSTMENT
}
