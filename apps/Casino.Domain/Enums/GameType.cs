namespace Casino.Domain.Enums;

/// <summary>
/// Tipo de juego para clasificación principal
/// Solo dos tipos fundamentales: slots y casino en vivo
/// </summary>
public enum GameType
{
    /// <summary>
    /// Juegos de slots/máquinas tragamonedas (RNG)
    /// Incluye: video slots, classic slots, megaways, etc.
    /// </summary>
    SLOT,
    
    /// <summary>
    /// Juegos de casino en vivo con dealers reales
    /// Incluye: ruleta en vivo, blackjack en vivo, baccarat, etc.
    /// </summary>
    LIVE_CASINO
}
