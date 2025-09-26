namespace Casino.Domain.Entities;

public class Wallet
{
    public Guid PlayerId { get; set; }
    public long BalanceBigint { get; set; } = 0;
    
    // Navigation properties
    public Player Player { get; set; } = null!;
}