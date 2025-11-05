using System.ComponentModel.DataAnnotations;

namespace Casino.Domain.Users;

public class User
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Username { get; set; } = default!;

    [MaxLength(100)]
    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public Role Role { get; set; } = Role.PLAYER;

    public decimal Balance { get; set; } = 0m;

    // Propiedades para jerarquía
    public int? ParentUserId { get; set; }
    public User? ParentUser { get; set; }
    public List<User> ChildUsers { get; set; } = new();

    // Propiedades para comisiones (solo para cajeros)
    public decimal CommissionRate { get; set; } = 0m; // Porcentaje de comisión (0-100)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
