using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(CasinoDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BalanceResponse> GetBalanceAsync(BalanceRequest request)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);

        return new BalanceResponse(wallet?.BalanceBigint ?? 0);
    }

    public async Task<WalletOperationResponse> DebitAsync(DebitRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Check for idempotency
            var existingEntry = await _context.Ledger
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ExternalRef == request.ExternalRef);

            if (existingEntry != null)
            {
                _logger.LogInformation("Debit operation is idempotent for ExternalRef: {ExternalRef}", request.ExternalRef);
                var currentBalance = await GetCurrentBalanceAsync(request.PlayerId);
                return new WalletOperationResponse(true, currentBalance, existingEntry.Id);
            }

            // Get wallet with row lock
            var wallet = await _context.Wallets
                .FromSql($"SELECT * FROM \"Wallets\" WHERE \"PlayerId\" = {request.PlayerId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (wallet == null)
            {
                return new WalletOperationResponse(false, 0, ErrorMessage: "Wallet not found");
            }

            // Check sufficient balance
            if (wallet.BalanceBigint < request.Amount)
            {
                return new WalletOperationResponse(false, wallet.BalanceBigint, ErrorMessage: "Insufficient balance");
            }

            // Get player info for ledger
            var player = await _context.Players
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

            if (player == null)
            {
                return new WalletOperationResponse(false, wallet.BalanceBigint, ErrorMessage: "Player not found");
            }

            // Update balance
            wallet.BalanceBigint -= request.Amount;

            // Create ledger entry
            var ledgerEntry = new Ledger
            {
                OperatorId = player.Brand.OperatorId,
                BrandId = player.BrandId,
                PlayerId = request.PlayerId,
                DeltaBigint = -request.Amount,
                Reason = request.Reason,
                RoundId = request.RoundId,
                GameCode = request.GameCode,
                Provider = request.Provider,
                ExternalRef = request.ExternalRef,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Debit successful for Player: {PlayerId}, Amount: {Amount}, Balance: {Balance}", 
                request.PlayerId, request.Amount, wallet.BalanceBigint);

            return new WalletOperationResponse(true, wallet.BalanceBigint, ledgerEntry.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during debit operation for Player: {PlayerId}", request.PlayerId);
            throw;
        }
    }

    public async Task<WalletOperationResponse> CreditAsync(CreditRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Check for idempotency
            var existingEntry = await _context.Ledger
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ExternalRef == request.ExternalRef);

            if (existingEntry != null)
            {
                _logger.LogInformation("Credit operation is idempotent for ExternalRef: {ExternalRef}", request.ExternalRef);
                var currentBalance = await GetCurrentBalanceAsync(request.PlayerId);
                return new WalletOperationResponse(true, currentBalance, existingEntry.Id);
            }

            // Get wallet with row lock
            var wallet = await _context.Wallets
                .FromSql($"SELECT * FROM \"Wallets\" WHERE \"PlayerId\" = {request.PlayerId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (wallet == null)
            {
                return new WalletOperationResponse(false, 0, ErrorMessage: "Wallet not found");
            }

            // Get player info for ledger
            var player = await _context.Players
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

            if (player == null)
            {
                return new WalletOperationResponse(false, wallet.BalanceBigint, ErrorMessage: "Player not found");
            }

            // Update balance
            wallet.BalanceBigint += request.Amount;

            // Create ledger entry
            var ledgerEntry = new Ledger
            {
                OperatorId = player.Brand.OperatorId,
                BrandId = player.BrandId,
                PlayerId = request.PlayerId,
                DeltaBigint = request.Amount,
                Reason = request.Reason,
                RoundId = request.RoundId,
                GameCode = request.GameCode,
                Provider = request.Provider,
                ExternalRef = request.ExternalRef,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Credit successful for Player: {PlayerId}, Amount: {Amount}, Balance: {Balance}", 
                request.PlayerId, request.Amount, wallet.BalanceBigint);

            return new WalletOperationResponse(true, wallet.BalanceBigint, ledgerEntry.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during credit operation for Player: {PlayerId}", request.PlayerId);
            throw;
        }
    }

    public async Task<WalletOperationResponse> RollbackAsync(RollbackRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Find the original ledger entry
            var originalEntry = await _context.Ledger
                .FirstOrDefaultAsync(l => l.ExternalRef == request.ExternalRefOriginal);

            if (originalEntry == null)
            {
                return new WalletOperationResponse(false, 0, ErrorMessage: "Original transaction not found");
            }

            // Check if already rolled back
            var rollbackEntry = await _context.Ledger
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Reason == LedgerReason.ROLLBACK && 
                                         l.ExternalRef == $"ROLLBACK_{request.ExternalRefOriginal}");

            if (rollbackEntry != null)
            {
                _logger.LogInformation("Rollback operation is idempotent for ExternalRef: {ExternalRef}", request.ExternalRefOriginal);
                var currentBalance = await GetCurrentBalanceAsync(originalEntry.PlayerId);
                return new WalletOperationResponse(true, currentBalance, rollbackEntry.Id);
            }

            // Get wallet with row lock
            var wallet = await _context.Wallets
                .FromSql($"SELECT * FROM \"Wallets\" WHERE \"PlayerId\" = {originalEntry.PlayerId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (wallet == null)
            {
                return new WalletOperationResponse(false, 0, ErrorMessage: "Wallet not found");
            }

            // Reverse the original operation
            wallet.BalanceBigint -= originalEntry.DeltaBigint;

            // Ensure balance doesn't go negative
            if (wallet.BalanceBigint < 0)
            {
                return new WalletOperationResponse(false, wallet.BalanceBigint + originalEntry.DeltaBigint, 
                    ErrorMessage: "Rollback would result in negative balance");
            }

            // Create rollback ledger entry
            var ledgerEntry = new Ledger
            {
                OperatorId = originalEntry.OperatorId,
                BrandId = originalEntry.BrandId,
                PlayerId = originalEntry.PlayerId,
                DeltaBigint = -originalEntry.DeltaBigint,
                Reason = LedgerReason.ROLLBACK,
                RoundId = originalEntry.RoundId,
                GameCode = originalEntry.GameCode,
                Provider = originalEntry.Provider,
                ExternalRef = $"ROLLBACK_{request.ExternalRefOriginal}",
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Rollback successful for ExternalRef: {ExternalRef}, Balance: {Balance}", 
                request.ExternalRefOriginal, wallet.BalanceBigint);

            return new WalletOperationResponse(true, wallet.BalanceBigint, ledgerEntry.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during rollback operation for ExternalRef: {ExternalRef}", request.ExternalRefOriginal);
            throw;
        }
    }

    private async Task<long> GetCurrentBalanceAsync(Guid playerId)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.PlayerId == playerId);
        
        return wallet?.BalanceBigint ?? 0;
    }
}