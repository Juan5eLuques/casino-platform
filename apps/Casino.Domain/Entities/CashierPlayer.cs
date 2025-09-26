namespace Casino.Domain.Entities;

public class CashierPlayer
{
    public Guid CashierId { get; set; }
    public Guid PlayerId { get; set; }
    public DateTime AssignedAt { get; set; }
    
    // Navigation properties
    public BackofficeUser Cashier { get; set; } = null!;
    public Player Player { get; set; } = null!;
}