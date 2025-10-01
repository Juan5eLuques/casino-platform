using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Casino.Infrastructure.Data;

public class CasinoDbContext : DbContext
{
    public CasinoDbContext(DbContextOptions<CasinoDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Operator> Operators { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Ledger> Ledger { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<BrandGame> BrandGames { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<Round> Rounds { get; set; }
    public DbSet<BackofficeUser> BackofficeUsers { get; set; }
    public DbSet<CashierPlayer> CashierPlayers { get; set; }
    public DbSet<BackofficeAudit> BackofficeAudits { get; set; }
    public DbSet<ProviderAudit> ProviderAudits { get; set; }
    public DbSet<BrandProviderConfig> BrandProviderConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Operator configuration
        modelBuilder.Entity<Operator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Brand configuration
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Domain).IsUnique().HasFilter("\"Domain\" IS NOT NULL");
            entity.HasIndex(e => e.AdminDomain).IsUnique().HasFilter("\"AdminDomain\" IS NOT NULL");
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Locale).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Domain).HasMaxLength(255);
            entity.Property(e => e.AdminDomain).HasMaxLength(255);
            entity.Property(e => e.CorsOrigins)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<string[]>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToArray()));
            entity.Property(e => e.Theme)
                .HasColumnType("jsonb");
            entity.Property(e => e.Settings)
                .HasColumnType("jsonb");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Operator)
                .WithMany(o => o.Brands)
                .HasForeignKey(e => e.OperatorId);
        });

        // Player configuration
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BrandId, e.Username }).IsUnique();
            entity.HasIndex(e => new { e.BrandId, e.ExternalId }).IsUnique()
                .HasFilter("external_id IS NOT NULL");
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.Players)
                .HasForeignKey(e => e.BrandId);
        });

        // Wallet configuration
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.PlayerId);
            entity.Property(e => e.BalanceBigint).HasDefaultValue(0);
            
            entity.HasOne(e => e.Player)
                .WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(e => e.PlayerId);
        });

        // Ledger configuration
        modelBuilder.Entity<Ledger>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.HasIndex(e => e.ExternalRef).IsUnique()
                .HasFilter("external_ref IS NOT NULL");
            entity.HasIndex(e => new { e.PlayerId, e.Id }).HasDatabaseName("IX_Ledger_PlayerId_Id_Desc");
            entity.HasIndex(e => e.RoundId);
            
            entity.Property(e => e.Reason).HasConversion<string>();
            entity.Property(e => e.GameCode).HasMaxLength(100);
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.ExternalRef).HasMaxLength(255);
            entity.Property(e => e.Meta)
                .HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Operator)
                .WithMany(o => o.LedgerEntries)
                .HasForeignKey(e => e.OperatorId);
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.LedgerEntries)
                .HasForeignKey(e => e.BrandId);
            entity.HasOne(e => e.Player)
                .WithMany(p => p.LedgerEntries)
                .HasForeignKey(e => e.PlayerId);
            entity.HasOne(e => e.Round)
                .WithMany(r => r.LedgerEntries)
                .HasForeignKey(e => e.RoundId);
        });

        // Game configuration
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // BrandGame configuration
        modelBuilder.Entity<BrandGame>(entity =>
        {
            entity.HasKey(e => new { e.BrandId, e.GameId });
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<string[]>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToArray()));
            
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.BrandGames)
                .HasForeignKey(e => e.BrandId);
            entity.HasOne(e => e.Game)
                .WithMany(g => g.BrandGames)
                .HasForeignKey(e => e.GameId);
        });

        // GameSession configuration
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GameCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Player)
                .WithMany(p => p.GameSessions)
                .HasForeignKey(e => e.PlayerId);
        });

        // Round configuration
        modelBuilder.Entity<Round>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Rounds)
                .HasForeignKey(e => e.SessionId);
        });

        // BackofficeUser configuration
        modelBuilder.Entity<BackofficeUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastLoginAt); // Nullable DateTime
            
            entity.HasOne(e => e.Operator)
                .WithMany(o => o.BackofficeUsers)
                .HasForeignKey(e => e.OperatorId);
        });

        // CashierPlayer configuration
        modelBuilder.Entity<CashierPlayer>(entity =>
        {
            entity.HasKey(e => new { e.CashierId, e.PlayerId });
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Cashier)
                .WithMany(c => c.CashierPlayers)
                .HasForeignKey(e => e.CashierId);
            entity.HasOne(e => e.Player)
                .WithMany(p => p.CashierPlayers)
                .HasForeignKey(e => e.PlayerId);
        });

        // BackofficeAudit configuration
        modelBuilder.Entity<BackofficeAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TargetType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TargetId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Meta)
                .HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.BackofficeAudits)
                .HasForeignKey(e => e.UserId);
        });

        // ProviderAudit configuration
        modelBuilder.Entity<ProviderAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RequestData)
                .HasColumnType("jsonb");
            entity.Property(e => e.ResponseData)
                .HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // BrandProviderConfig configuration
        modelBuilder.Entity<BrandProviderConfig>(entity =>
        {
            entity.HasKey(e => new { e.BrandId, e.ProviderCode });
            entity.Property(e => e.ProviderCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Secret).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Meta)
                .HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.ProviderConfigs)
                .HasForeignKey(e => e.BrandId);
        });
    }
}