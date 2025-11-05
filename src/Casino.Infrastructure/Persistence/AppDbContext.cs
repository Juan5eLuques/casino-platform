using Casino.Domain.Users;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Casino.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Transfer> Transfers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registra el enum de PostgreSQL
        modelBuilder.HasPostgresEnum<Role>();

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);

            b.Property(u => u.Id).HasColumnName("id");
            b.Property(u => u.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
            b.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(100);
            b.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            b.Property(u => u.Role)
                .HasColumnName("role")
                .HasConversion<string>() // <--- Esto es clave
                .IsRequired();
            b.Property(u => u.Balance).HasColumnName("balance").IsRequired().HasDefaultValue(0);
            b.Property(u => u.ParentUserId).HasColumnName("parent_user_id");
            b.Property(u => u.CommissionRate).HasColumnName("commission_rate").HasDefaultValue(0);
            b.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(u => u.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configurar relación padre-hijo
            b.HasOne(u => u.ParentUser)
                .WithMany(u => u.ChildUsers)
                .HasForeignKey(u => u.ParentUserId)
                .OnDelete(DeleteBehavior.Restrict); // Evitar eliminaciones en cascada

            // Índices únicos
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Transfer>(b =>
        {
            b.ToTable("transfers"); // <-- minúsculas, igual que en la base de datos
            b.HasKey(t => t.Id);
            // ...otros mapeos si es necesario
        });

        base.OnModelCreating(modelBuilder);
    }
}

