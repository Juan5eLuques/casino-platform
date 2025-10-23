using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class CommissionService : ICommissionService
{
    private readonly CasinoDbContext _db;
    private readonly IHierarchyService _hierarchyService;
    private readonly IConfiguration _config;
    private readonly ILogger<CommissionService> _logger;
    
    public CommissionService(
        CasinoDbContext db,
        IHierarchyService hierarchyService,
        IConfiguration config,
        ILogger<CommissionService> logger)
    {
        _db = db;
        _hierarchyService = hierarchyService;
        _config = config;
        _logger = logger;
    }
    
    public async Task AccrueCommissionsFromNetWinAsync(
        Guid playerId,
        long netWinAmount,
        Guid? roundId = null,
        CancellationToken cancellationToken = default)
    {
        // Feature flag check
        if (!_config.GetValue<bool>("Features:EnableCommissionAccrual"))
        {
            _logger.LogDebug("Commission accrual disabled by feature flag");
            return;
        }
        
        if (netWinAmount <= 0)
        {
            _logger.LogDebug("NetWin amount {Amount} is not positive, skipping commission", netWinAmount);
            return;
        }
        
        // Obtener player y su creador
        var player = await _db.Players
            .AsNoTracking()
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
        
        if (player?.CreatedByUserId == null)
        {
            _logger.LogWarning("Player {PlayerId} not found or has no creator", playerId);
            return;
        }
        
        // Obtener cadena jerárquica (cashier ? admin ? super_admin)
        var ancestors = await _hierarchyService.GetAncestorsAsync(
            player.CreatedByUserId.Value, 
            cancellationToken);
        
        if (!ancestors.Any())
        {
            _logger.LogWarning("No ancestors found for player creator {UserId}", player.CreatedByUserId);
            return;
        }
        
        var period = DateTime.UtcNow;
        long accumulatedCommission = 0;
        var accruals = new List<CommissionAccrual>();
        
        // Calcular comisiones en cascada (de abajo hacia arriba)
        foreach (var ancestor in ancestors.OrderBy(a => a.HierarchyLevel))
        {
            if (ancestor.CommissionPercent <= 0)
            {
                _logger.LogDebug("User {UserId} has 0% commission, skipping", ancestor.Id);
                continue;
            }
            
            // Comisión total de este nivel = NetWin * %
            var levelCommission = (long)(netWinAmount * (ancestor.CommissionPercent / 100m));
            
            // Comisión efectiva = diferencia con nivel inferior
            var effectiveCommission = levelCommission - accumulatedCommission;
            accumulatedCommission = levelCommission;
            
            if (effectiveCommission <= 0)
            {
                _logger.LogDebug(
                    "Effective commission for user {UserId} is {Amount}, skipping",
                    ancestor.Id, effectiveCommission);
                continue;
            }
            
            // Crear accrual
            var accrual = new CommissionAccrual
            {
                Id = Guid.NewGuid(),
                BrandId = player.BrandId,
                UserId = ancestor.Id,
                ParentUserId = ancestor.ParentAdminId,
                PeriodMonth = period.Month,
                PeriodYear = period.Year,
                BaseAmount = netWinAmount,
                CommissionRate = ancestor.CommissionPercent / 100m,
                CommissionAmount = effectiveCommission,
                SourceType = "NETWIN",
                SourceRoundId = roundId,
                SourcePlayerId = playerId,
                Settled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            accruals.Add(accrual);
            
            _logger.LogInformation(
                "Commission accrued: User {UserId} ({Role}, Level {Level}), " +
                "NetWin {NetWin}, Rate {Rate}%, Effective {Effective}",
                ancestor.Id, ancestor.Role, ancestor.HierarchyLevel,
                netWinAmount, ancestor.CommissionPercent, effectiveCommission);
        }
        
        if (accruals.Any())
        {
            _db.CommissionAccruals.AddRange(accruals);
            await _db.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Total {Count} commissions accrued for player {PlayerId}, " +
                "NetWin {NetWin}, Total commission {Total}",
                accruals.Count, playerId, netWinAmount, accruals.Sum(a => a.CommissionAmount));
        }
    }
    
    public async Task AccrueCommissionFromTransactionAsync(
        Guid transactionId,
        Guid userId,
        long commissionAmount,
        string commissionType,
        CancellationToken cancellationToken = default)
    {
        if (!_config.GetValue<bool>("Features:EnableCommissionAccrual"))
        {
            _logger.LogDebug("Commission accrual disabled by feature flag");
            return;
        }
        
        if (commissionAmount <= 0)
        {
            _logger.LogDebug("Commission amount {Amount} is not positive", commissionAmount);
            return;
        }
        
        var transaction = await _db.WalletTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
        
        if (transaction == null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
            return;
        }
        
        var user = await _db.BackofficeUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return;
        }
        
        var period = DateTime.UtcNow;
        
        var accrual = new CommissionAccrual
        {
            Id = Guid.NewGuid(),
            BrandId = transaction.BrandId,
            UserId = userId,
            ParentUserId = user.ParentAdminId,
            PeriodMonth = period.Month,
            PeriodYear = period.Year,
            BaseAmount = (long)transaction.Amount,
            CommissionRate = user.CommissionPercent / 100m,
            CommissionAmount = commissionAmount,
            SourceType = commissionType,
            SourceTransactionId = transactionId,
            Settled = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _db.CommissionAccruals.Add(accrual);
        await _db.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Commission accrued from transaction: User {UserId}, Type {Type}, Amount {Amount}",
            userId, commissionType, commissionAmount);
    }
    
    public async Task<IEnumerable<CommissionAccrual>> GetPendingCommissionsAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _db.CommissionAccruals
            .AsNoTracking()
            .Include(ca => ca.User)
            .Include(ca => ca.SourceTransaction)
            .Include(ca => ca.SourceRound)
            .Include(ca => ca.SourcePlayer)
            .Where(ca => ca.UserId == userId
                      && ca.PeriodYear == year
                      && ca.PeriodMonth == month
                      && !ca.Settled)
            .OrderBy(ca => ca.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<long> GetPendingCommissionsTotalAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _db.CommissionAccruals
            .Where(ca => ca.UserId == userId
                      && ca.PeriodYear == year
                      && ca.PeriodMonth == month
                      && !ca.Settled)
            .SumAsync(ca => ca.CommissionAmount, cancellationToken);
    }
    
    public async Task<CommissionSettlementResult> SettleCommissionsForPeriodAsync(
        Guid brandId,
        int year,
        int month,
        Guid settledByUserId,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            _logger.LogInformation(
                "Starting commission settlement for brand {BrandId}, period {Year}-{Month:D2}",
                brandId, year, month);
            
            // Obtener comisiones pendientes agrupadas por usuario
            var pendingCommissions = await _db.CommissionAccruals
                .Where(ca => ca.BrandId == brandId
                          && ca.PeriodYear == year
                          && ca.PeriodMonth == month
                          && !ca.Settled)
                .GroupBy(ca => new { ca.UserId, ca.User.Username })
                .Select(g => new
                {
                    g.Key.UserId,
                    g.Key.Username,
                    TotalCommission = g.Sum(ca => ca.CommissionAmount),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);
            
            if (!pendingCommissions.Any())
            {
                _logger.LogInformation("No pending commissions found for settlement");
                await transaction.CommitAsync(cancellationToken);
                return new CommissionSettlementResult
                {
                    Success = true,
                    TotalUsersSettled = 0,
                    TotalAmountSettled = 0
                };
            }
            
            var userSettlements = new List<UserSettlement>();
            
            foreach (var commission in pendingCommissions)
            {
                // Crear WalletTransaction de tipo COMMISSION
                var walletTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    BrandId = brandId,
                    FromUserId = null,  // De "house/treasury"
                    FromUserType = null,
                    ToUserId = commission.UserId,
                    ToUserType = "BACKOFFICE",
                    Amount = commission.TotalCommission,
                    TransactionType = TransactionType.COMMISSION,
                    IdempotencyKey = $"commission_settlement_{brandId}_{year}_{month:D2}_{commission.UserId}",
                    CreatedByUserId = settledByUserId,
                    CreatedByRole = "SUPER_ADMIN",
                    Notes = $"Commission settlement for {year}-{month:D2} ({commission.Count} accruals)",
                    CreatedAt = DateTime.UtcNow
                };
                
                _db.WalletTransactions.Add(walletTransaction);
                
                // Actualizar balance del usuario
                var user = await _db.BackofficeUsers
                    .FirstOrDefaultAsync(u => u.Id == commission.UserId, cancellationToken);
                
                if (user != null)
                {
                    user.WalletBalance += commission.TotalCommission;
                }
                
                // Marcar comisiones como liquidadas
                await _db.CommissionAccruals
                    .Where(ca => ca.UserId == commission.UserId
                              && ca.PeriodYear == year
                              && ca.PeriodMonth == month
                              && !ca.Settled)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(ca => ca.Settled, true)
                            .SetProperty(ca => ca.SettledAt, DateTime.UtcNow)
                            .SetProperty(ca => ca.SettledTransactionId, walletTransaction.Id)
                            .SetProperty(ca => ca.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);
                
                userSettlements.Add(new UserSettlement
                {
                    UserId = commission.UserId,
                    Username = commission.Username,
                    TotalCommission = commission.TotalCommission,
                    TransactionId = walletTransaction.Id,
                    CommissionCount = commission.Count
                });
                
                _logger.LogInformation(
                    "Settled commission: User {UserId} ({Username}), Amount {Amount}, Transaction {TxId}",
                    commission.UserId, commission.Username, commission.TotalCommission, walletTransaction.Id);
            }
            
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            var result = new CommissionSettlementResult
            {
                Success = true,
                TotalUsersSettled = userSettlements.Count,
                TotalAmountSettled = userSettlements.Sum(us => us.TotalCommission),
                UserSettlements = userSettlements
            };
            
            _logger.LogInformation(
                "Commission settlement completed: {Users} users, Total amount {Total}",
                result.TotalUsersSettled, result.TotalAmountSettled);
            
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            _logger.LogError(ex,
                "Failed to settle commissions for brand {BrandId}, period {Year}-{Month}",
                brandId, year, month);
            
            return new CommissionSettlementResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    public async Task<CommissionBreakdown> CalculateCommissionBreakdownAsync(
        Guid playerId,
        long netWinAmount,
        CancellationToken cancellationToken = default)
    {
        var breakdown = new CommissionBreakdown
        {
            PlayerId = playerId,
            NetWinAmount = netWinAmount
        };
        
        if (netWinAmount <= 0)
        {
            return breakdown;
        }
        
        // Obtener player y su creador
        var player = await _db.Players
            .AsNoTracking()
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
        
        if (player?.CreatedByUserId == null)
        {
            return breakdown;
        }
        
        // Obtener cadena jerárquica
        var ancestors = await _hierarchyService.GetAncestorsAsync(
            player.CreatedByUserId.Value,
            cancellationToken);
        
        long accumulatedCommission = 0;
        
        foreach (var ancestor in ancestors.OrderBy(a => a.HierarchyLevel))
        {
            if (ancestor.CommissionPercent <= 0) continue;
            
            var levelCommission = (long)(netWinAmount * (ancestor.CommissionPercent / 100m));
            var effectiveCommission = levelCommission - accumulatedCommission;
            accumulatedCommission = levelCommission;
            
            if (effectiveCommission <= 0) continue;
            
            breakdown.Levels.Add(new LevelCommission
            {
                UserId = ancestor.Id,
                Username = ancestor.Username,
                Role = ancestor.Role.ToString(),
                HierarchyLevel = ancestor.HierarchyLevel,
                CommissionRate = ancestor.CommissionPercent / 100m,
                CommissionAmount = levelCommission,
                EffectiveCommission = effectiveCommission
            });
        }
        
        return breakdown;
    }
}
