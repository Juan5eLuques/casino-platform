using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casino.Api.Endpoints;

/// <summary>
/// Endpoint de diagnóstico para verificar estado del sistema
/// </summary>
public static class DiagnosticEndpoints
{
    public static void MapDiagnosticEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/diagnostics")
            .WithTags("Diagnostics")
            .RequireAuthorization("SuperAdminOnly"); // FIX: Changed from SuperAdmin to SuperAdminOnly

        group.MapGet("/system-status", GetSystemStatus)
            .WithName("GetSystemStatus")
     .WithSummary("Get complete system status for diagnostics");

        group.MapPost("/reset-and-initialize", ResetAndInitialize)
            .WithName("ResetAndInitialize")
   .WithSummary("Reset all users and transactions, then initialize with proper structure");
    }

private static async Task<IResult> GetSystemStatus(
        CasinoDbContext context,
        ILogger<Program> logger)
    {
        try
    {
            var status = new
          {
    Brands = await context.Brands
      .Select(b => new
             {
         b.Id,
        b.Code,
     b.Name,
             b.Status
     })
           .ToListAsync(),

                BackofficeUsers = await context.BackofficeUsers
                    .Select(u => new
      {
      u.Id,
       u.Username,
          u.Role,
      u.WalletBalance,
                 u.BrandId,
         BrandName = u.Brand != null ? u.Brand.Name : "N/A",
         u.ParentAdminId,
            ParentAdminUsername = u.ParentAdmin != null ? u.ParentAdmin.Username : null,
         u.ParentCashierId,
      ParentCashierUsername = u.ParentCashier != null ? u.ParentCashier.Username : null,
   u.CommissionPercent,
        u.HierarchyLevel,
            u.CreatedByUserId,
        u.CreatedAt
   })
        .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
     .ToListAsync(),

      Players = await context.Players
         .Select(p => new
   {
      p.Id,
      p.Username,
      p.WalletBalance,
  p.Status,
       p.BrandId,
         BrandName = p.Brand != null ? p.Brand.Name : "N/A",
            p.CreatedByUserId,
    CreatedByUsername = p.CreatedByUser != null ? p.CreatedByUser.Username : "N/A",
    p.CreatedAt
        })
    .OrderBy(p => p.Username)
           .ToListAsync(),

    WalletTransactions = await context.WalletTransactions
         .Select(t => new
        {
        t.Id,
   t.TransactionType,
t.FromUserId,
            t.FromUserType,
            t.ToUserId,
     t.ToUserType,
       t.Amount,
          t.Description,
             t.CreatedByUserId,
      t.CreatedByRole,
    t.CreatedAt
           })
 .OrderByDescending(t => t.CreatedAt)
      .Take(50)
          .ToListAsync(),

     Ledger = await context.Ledger
                 .Select(l => new
 {
          l.Id,
  l.PlayerId,
        l.DeltaBigint,
    l.Reason,
   l.RoundId,
             l.GameCode,
l.Provider,
        l.CreatedAt
  })
   .OrderByDescending(l => l.CreatedAt)
             .Take(50)
 .ToListAsync(),

     Summary = new
   {
    TotalBrands = await context.Brands.CountAsync(),
      TotalBackofficeUsers = await context.BackofficeUsers.CountAsync(),
         SuperAdmins = await context.BackofficeUsers.CountAsync(u => u.Role == Domain.Enums.BackofficeUserRole.SUPER_ADMIN),
       BrandAdmins = await context.BackofficeUsers.CountAsync(u => u.Role == Domain.Enums.BackofficeUserRole.BRAND_ADMIN),
     Cashiers = await context.BackofficeUsers.CountAsync(u => u.Role == Domain.Enums.BackofficeUserRole.CASHIER),
     TotalPlayers = await context.Players.CountAsync(),
     PlayersWithoutCreator = await context.Players.CountAsync(p => p.CreatedByUserId == null),
             TotalWalletTransactions = await context.WalletTransactions.CountAsync(),
     TotalLedgerEntries = await context.Ledger.CountAsync(),
         TotalBalanceBackoffice = await context.BackofficeUsers.SumAsync(u => u.WalletBalance),
    TotalBalancePlayers = await context.Players.SumAsync(p => p.WalletBalance)
          }
};

  return Results.Ok(status);
        }
        catch (Exception ex)
     {
     logger.LogError(ex, "Error getting system status");
return Results.Problem("Error getting system status", statusCode: 500);
        }
    }

    private static async Task<IResult> ResetAndInitialize(
        CasinoDbContext context,
     ILogger<Program> logger)
    {
     try
        {
            logger.LogWarning("Starting system reset and initialization");

            await using var transaction = await context.Database.BeginTransactionAsync();

            try
     {
           // 1. Delete all existing data (maintaining referential integrity)
       logger.LogInformation("Deleting existing data...");
         
    await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Ledger\"");
           await context.Database.ExecuteSqlRawAsync("DELETE FROM \"WalletTransactions\"");
      await context.Database.ExecuteSqlRawAsync("DELETE FROM \"CommissionAccruals\"");
     await context.Database.ExecuteSqlRawAsync("DELETE FROM \"CashierPlayers\"");
      await context.Database.ExecuteSqlRawAsync("DELETE FROM \"GameSessions\"");
     await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Rounds\"");
             await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Wallets\"");
         await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Players\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"BackofficeUsers\"");
                
      logger.LogInformation("All data deleted successfully");

     // 2. Get or create default brand
         var brand = await context.Brands.FirstOrDefaultAsync(b => b.Code == "bet30");
   if (brand == null)
         {
         logger.LogInformation("Creating default brand 'bet30'");
        brand = new Domain.Entities.Brand
        {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
    Code = "bet30",
 Name = "Bet30 Casino",
    Locale = "es-AR",
                  Domain = "bet30.local",
    AdminDomain = "admin.bet30.local",
          CorsOrigins = new[] { "http://localhost:5173", "http://admin.bet30.local:5173", "https://admin.bet30.local:5173" },
   Status = Domain.Enums.BrandStatus.ACTIVE,
         CreatedAt = DateTime.UtcNow,
     UpdatedAt = DateTime.UtcNow
          };
           context.Brands.Add(brand);
    await context.SaveChangesAsync();
   }

         // 3. Create SUPER_ADMIN
         logger.LogInformation("Creating SUPER_ADMIN");
 var superAdminId = Guid.NewGuid();
       var superAdmin = new Domain.Entities.BackofficeUser
             {
        Id = superAdminId,
     Username = "superadmin",
     PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = Domain.Enums.BackofficeUserRole.SUPER_ADMIN,
   Status = Domain.Enums.BackofficeUserStatus.ACTIVE,
        BrandId = brand.Id,
     WalletBalance = 10000.00m, // $10,000 initial balance
            CommissionPercent = 0,
       HierarchyLevel = 0,
 HierarchyPath = "",
         CreatedByUserId = null, // Self-created
        CreatedAt = DateTime.UtcNow
         };
     context.BackofficeUsers.Add(superAdmin);
   await context.SaveChangesAsync();

   // 4. Create MINT transaction for SUPER_ADMIN
        logger.LogInformation("Creating MINT transaction for SUPER_ADMIN");
           var mintTransaction = new Domain.Entities.WalletTransaction
  {
               Id = Guid.NewGuid(),
   BrandId = brand.Id,
   FromUserId = null,
        FromUserType = null,
       ToUserId = superAdminId,
      ToUserType = "BACKOFFICE",
           Amount = 10000.00m,
   TransactionType = Domain.Enums.TransactionType.MINT,
             PreviousBalanceFrom = null,
              NewBalanceFrom = null,
  PreviousBalanceTo = 0,
   NewBalanceTo = 10000.00m,
Description = "Initial MINT for SUPER_ADMIN",
   CreatedByUserId = superAdminId,
      CreatedByRole = "SUPER_ADMIN",
           IdempotencyKey = $"init-mint-superadmin-{superAdminId}",
      CreatedAt = DateTime.UtcNow
            };
                context.WalletTransactions.Add(mintTransaction);
         await context.SaveChangesAsync();

   // 5. Create 2 Cashiers
    logger.LogInformation("Creating Cashiers");
            var cashier1Id = Guid.NewGuid();
   var cashier1 = new Domain.Entities.BackofficeUser
    {
         Id = cashier1Id,
           Username = "cashier1",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
           Role = Domain.Enums.BackofficeUserRole.CASHIER,
    Status = Domain.Enums.BackofficeUserStatus.ACTIVE,
          BrandId = brand.Id,
        WalletBalance = 2000.00m, // $2,000
           ParentAdminId = superAdminId,
          CommissionPercent = 10,
     HierarchyLevel = 1,
        HierarchyPath = superAdminId.ToString(),
    CreatedByUserId = superAdminId,
        CreatedAt = DateTime.UtcNow
     };
    context.BackofficeUsers.Add(cashier1);

 var cashier2Id = Guid.NewGuid();
        var cashier2 = new Domain.Entities.BackofficeUser
           {
   Id = cashier2Id,
     Username = "cashier2",
PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
           Role = Domain.Enums.BackofficeUserRole.CASHIER,
        Status = Domain.Enums.BackofficeUserStatus.ACTIVE,
    BrandId = brand.Id,
   WalletBalance = 3000.00m, // $3,000
       ParentAdminId = superAdminId,
 CommissionPercent = 15,
          HierarchyLevel = 1,
   HierarchyPath = superAdminId.ToString(),
              CreatedByUserId = superAdminId,
       CreatedAt = DateTime.UtcNow
          };
                context.BackofficeUsers.Add(cashier2);
        await context.SaveChangesAsync();

    // 6. Create TRANSFER transactions for Cashiers
      logger.LogInformation("Creating TRANSFER transactions for Cashiers");
       var transferCashier1 = new Domain.Entities.WalletTransaction
      {
     Id = Guid.NewGuid(),
     BrandId = brand.Id,
   FromUserId = superAdminId,
      FromUserType = "BACKOFFICE",
                    ToUserId = cashier1Id,
            ToUserType = "BACKOFFICE",
          Amount = 2000.00m,
            TransactionType = Domain.Enums.TransactionType.TRANSFER,
        PreviousBalanceFrom = 10000.00m,
              NewBalanceFrom = 8000.00m,
            PreviousBalanceTo = 0,
           NewBalanceTo = 2000.00m,
              Description = "Initial transfer to Cashier1",
           CreatedByUserId = superAdminId,
              CreatedByRole = "SUPER_ADMIN",
        IdempotencyKey = $"init-transfer-cashier1-{cashier1Id}",
      CreatedAt = DateTime.UtcNow
                };
 context.WalletTransactions.Add(transferCashier1);

     var transferCashier2 = new Domain.Entities.WalletTransaction
       {
     Id = Guid.NewGuid(),
        BrandId = brand.Id,
                    FromUserId = superAdminId,
         FromUserType = "BACKOFFICE",
        ToUserId = cashier2Id,
       ToUserType = "BACKOFFICE",
        Amount = 3000.00m,
             TransactionType = Domain.Enums.TransactionType.TRANSFER,
PreviousBalanceFrom = 8000.00m,
            NewBalanceFrom = 5000.00m,
        PreviousBalanceTo = 0,
            NewBalanceTo = 3000.00m,
    Description = "Initial transfer to Cashier2",
       CreatedByUserId = superAdminId,
   CreatedByRole = "SUPER_ADMIN",
           IdempotencyKey = $"init-transfer-cashier2-{cashier2Id}",
         CreatedAt = DateTime.UtcNow
     };
       context.WalletTransactions.Add(transferCashier2);
           await context.SaveChangesAsync();

    // 7. Update SUPER_ADMIN balance
     superAdmin.WalletBalance = 5000.00m;
  await context.SaveChangesAsync();

         // 8. Create 2 Players (1 per cashier)
       logger.LogInformation("Creating Players");
var player1Id = Guid.NewGuid();
         var player1 = new Domain.Entities.Player
   {
    Id = player1Id,
        BrandId = brand.Id,
       Username = "player1",
    Email = "player1@test.com",
     WalletBalance = 500.00m,
      Status = Domain.Enums.PlayerStatus.ACTIVE,
             CreatedByUserId = cashier1Id,
     CreatedAt = DateTime.UtcNow
       };
         context.Players.Add(player1);

      // Create Wallet for player1
    var wallet1 = new Domain.Entities.Wallet
     {
             PlayerId = player1Id,
                    BalanceBigint = 50000 // $500 in cents (for legacy compatibility)
      };
    context.Wallets.Add(wallet1);

           var player2Id = Guid.NewGuid();
                var player2 = new Domain.Entities.Player
     {
   Id = player2Id,
    BrandId = brand.Id,
        Username = "player2",
 Email = "player2@test.com",
  WalletBalance = 750.00m,
  Status = Domain.Enums.PlayerStatus.ACTIVE,
    CreatedByUserId = cashier2Id,
      CreatedAt = DateTime.UtcNow
        };
      context.Players.Add(player2);

           // Create Wallet for player2
          var wallet2 = new Domain.Entities.Wallet
              {
       PlayerId = player2Id,
              BalanceBigint = 75000 // $750 in cents
          };
  context.Wallets.Add(wallet2);
      await context.SaveChangesAsync();

   // 9. Create TRANSFER transactions for Players
    logger.LogInformation("Creating TRANSFER transactions for Players");
   var transferPlayer1 = new Domain.Entities.WalletTransaction
  {
         Id = Guid.NewGuid(),
           BrandId = brand.Id,
        FromUserId = cashier1Id,
          FromUserType = "BACKOFFICE",
               ToUserId = player1Id,
        ToUserType = "PLAYER",
    Amount = 500.00m,
            TransactionType = Domain.Enums.TransactionType.TRANSFER,
          PreviousBalanceFrom = 2000.00m,
       NewBalanceFrom = 1500.00m,
       PreviousBalanceTo = 0,
     NewBalanceTo = 500.00m,
   Description = "Initial transfer to Player1",
            CreatedByUserId = cashier1Id,
      CreatedByRole = "CASHIER",
 IdempotencyKey = $"init-transfer-player1-{player1Id}",
        CreatedAt = DateTime.UtcNow
      };
          context.WalletTransactions.Add(transferPlayer1);

        var transferPlayer2 = new Domain.Entities.WalletTransaction
                {
  Id = Guid.NewGuid(),
           BrandId = brand.Id,
        FromUserId = cashier2Id,
FromUserType = "BACKOFFICE",
           ToUserId = player2Id,
   ToUserType = "PLAYER",
         Amount = 750.00m,
     TransactionType = Domain.Enums.TransactionType.TRANSFER,
        PreviousBalanceFrom = 3000.00m,
    NewBalanceFrom = 2250.00m,
              PreviousBalanceTo = 0,
  NewBalanceTo = 750.00m,
               Description = "Initial transfer to Player2",
     CreatedByUserId = cashier2Id,
          CreatedByRole = "CASHIER",
             IdempotencyKey = $"init-transfer-player2-{player2Id}",
              CreatedAt = DateTime.UtcNow
        };
 context.WalletTransactions.Add(transferPlayer2);
       await context.SaveChangesAsync();

    // 10. Update Cashiers balances
        cashier1.WalletBalance = 1500.00m;
                cashier2.WalletBalance = 2250.00m;
          await context.SaveChangesAsync();

       // 11. Assign players to cashiers
        logger.LogInformation("Assigning players to cashiers");
          var assignment1 = new Domain.Entities.CashierPlayer
      {
      CashierId = cashier1Id,
      PlayerId = player1Id,
          AssignedAt = DateTime.UtcNow
    };
           context.CashierPlayers.Add(assignment1);

 var assignment2 = new Domain.Entities.CashierPlayer
            {
CashierId = cashier2Id,
    PlayerId = player2Id,
           AssignedAt = DateTime.UtcNow
           };
    context.CashierPlayers.Add(assignment2);
         await context.SaveChangesAsync();

      await transaction.CommitAsync();

            logger.LogInformation("System reset and initialization completed successfully");

   return Results.Ok(new
  {
         success = true,
     message = "System reset and initialized successfully",
    structure = new
        {
       brand = new { brand.Id, brand.Code, brand.Name },
        superAdmin = new { superAdmin.Id, superAdmin.Username, Balance = superAdmin.WalletBalance },
      cashiers = new[]
{
      new { cashier1.Id, cashier1.Username, Balance = cashier1.WalletBalance, Commission = cashier1.CommissionPercent },
       new { cashier2.Id, cashier2.Username, Balance = cashier2.WalletBalance, Commission = cashier2.CommissionPercent }
       },
              players = new[]
      {
      new { player1.Id, player1.Username, Balance = player1.WalletBalance, CreatedBy = "cashier1" },
     new { player2.Id, player2.Username, Balance = player2.WalletBalance, CreatedBy = "cashier2" }
            },
       transactions = new
 {
           Total = 5,
          MINT = 1,
       TRANSFER = 4
         },
      balances = new
       {
     SuperAdmin = 5000.00m,
            Cashier1 = 1500.00m,
    Cashier2 = 2250.00m,
     Player1 = 500.00m,
             Player2 = 750.00m,
    Total = 10000.00m
     }
     }
    });
    }
            catch (Exception ex)
            {
     await transaction.RollbackAsync();
throw;
        }
        }
 catch (Exception ex)
      {
    logger.LogError(ex, "Error during system reset and initialization");
     return Results.Problem($"Error: {ex.Message}", statusCode: 500);
        }
 }
}
