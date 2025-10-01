using Casino.Application.DTOs.Player;
using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class PlayerService : IPlayerService
{
    private readonly CasinoDbContext _context;
    private readonly IWalletService _walletService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(
        CasinoDbContext context,
        IWalletService walletService,
        IAuditService auditService,
        ILogger<PlayerService> logger)
    {
        _context = context;
        _walletService = walletService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetPlayerResponse> CreatePlayerAsync(CreatePlayerRequest request, Guid currentUserId)
    {
        // Verificar que la marca existe y está activa
        var brand = await _context.Brands
            .FirstOrDefaultAsync(b => b.Id == request.BrandId && b.Status == BrandStatus.ACTIVE);

        if (brand == null)
        {
            throw new InvalidOperationException("Brand not found or inactive");
        }

        // Verificar username único en la marca
        var existingPlayer = await _context.Players
            .FirstOrDefaultAsync(p => p.BrandId == request.BrandId && p.Username == request.Username);

        if (existingPlayer != null)
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists in brand '{brand.Code}'");
        }

        // Verificar external_id único en la marca si se proporciona
        if (!string.IsNullOrEmpty(request.ExternalId))
        {
            var existingExternalId = await _context.Players
                .FirstOrDefaultAsync(p => p.BrandId == request.BrandId && p.ExternalId == request.ExternalId);

            if (existingExternalId != null)
            {
                throw new InvalidOperationException($"ExternalId '{request.ExternalId}' already exists in brand '{brand.Code}'");
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Crear el jugador
            var newPlayer = new Player
            {
                Id = Guid.NewGuid(),
                BrandId = request.BrandId,
                Username = request.Username,
                Email = request.Email,
                ExternalId = request.ExternalId,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Players.Add(newPlayer);

            // Crear wallet
            var wallet = new Wallet
            {
                PlayerId = newPlayer.Id,
                BalanceBigint = 0
            };

            _context.Wallets.Add(wallet);

            await _context.SaveChangesAsync();

            // Si hay saldo inicial, hacer un ajuste
            if (request.InitialBalance > 0)
            {
                var creditRequest = new CreditRequest(
                    newPlayer.Id,
                    request.InitialBalance,
                    LedgerReason.ADMIN_GRANT,
                    null, // no roundId
                    $"initial_balance_{newPlayer.Id}",
                    null, // no gameCode
                    "SYSTEM");

                var creditResponse = await _walletService.CreditAsync(creditRequest);
                
                if (!creditResponse.Success)
                {
                    throw new InvalidOperationException($"Failed to set initial balance: {creditResponse.ErrorMessage}");
                }

                wallet.BalanceBigint = creditResponse.Balance;
            }

            await transaction.CommitAsync();

            // Cargar datos completos para la respuesta
            await _context.Entry(newPlayer)
                .Reference(p => p.Brand)
                .LoadAsync();

            await _context.Entry(newPlayer)
                .Reference(p => p.Wallet)
                .LoadAsync();

            await _auditService.LogBackofficeActionAsync(
                currentUserId,
                "CREATE_PLAYER",
                "Player",
                newPlayer.Id.ToString(),
                new { 
                    request.Username,
                    BrandCode = brand.Code,
                    request.Email,
                    request.ExternalId,
                    InitialBalance = request.InitialBalance,
                    request.Status 
                });

            _logger.LogInformation("Player created: {PlayerId} - {Username} in brand {BrandCode} by user {UserId}",
                newPlayer.Id, newPlayer.Username, brand.Code, currentUserId);

            return new GetPlayerResponse(
                newPlayer.Id,
                newPlayer.BrandId,
                newPlayer.Brand.Code,
                newPlayer.Brand.Name,
                newPlayer.Username,
                newPlayer.Email,
                newPlayer.ExternalId,
                newPlayer.Status,
                newPlayer.Wallet?.BalanceBigint ?? 0,
                newPlayer.CreatedAt);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<QueryPlayersResponse> GetPlayersAsync(QueryPlayersRequest request, Guid? operatorScope = null, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por operador
        if (operatorScope.HasValue)
        {
            query = query.Where(p => p.Brand.OperatorId == operatorScope.Value);
        }

        // Aplicar scope por brand (BrandContext)
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        // Aplicar filtros
        if (request.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == request.BrandId.Value);
        }

        if (!string.IsNullOrEmpty(request.Username))
        {
            query = query.Where(p => p.Username.Contains(request.Username));
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            query = query.Where(p => p.Email != null && p.Email.Contains(request.Email));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var players = await query
            .OrderBy(p => p.Username)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new GetPlayerResponse(
                p.Id,
                p.BrandId,
                p.Brand.Code,
                p.Brand.Name,
                p.Username,
                p.Email,
                p.ExternalId,
                p.Status,
                p.Wallet != null ? p.Wallet.BalanceBigint : 0,
                p.CreatedAt))
            .ToListAsync();

        return new QueryPlayersResponse(
            players,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }

    public async Task<GetPlayerResponse?> GetPlayerAsync(Guid playerId, Guid? operatorScope = null, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por operador
        if (operatorScope.HasValue)
        {
            query = query.Where(p => p.Brand.OperatorId == operatorScope.Value);
        }

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            return null;

        return new GetPlayerResponse(
            player.Id,
            player.BrandId,
            player.Brand.Code,
            player.Brand.Name,
            player.Username,
            player.Email,
            player.ExternalId,
            player.Status,
            player.Wallet?.BalanceBigint ?? 0,
            player.CreatedAt);
    }

    public async Task<GetPlayerResponse> UpdatePlayerAsync(Guid playerId, UpdatePlayerRequest request, Guid currentUserId, Guid? operatorScope = null, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por operador
        if (operatorScope.HasValue)
        {
            query = query.Where(p => p.Brand.OperatorId == operatorScope.Value);
        }

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
        {
            throw new InvalidOperationException("Player not found or access denied");
        }

        var changes = new Dictionary<string, object>();

        // Cambio de username
        if (!string.IsNullOrEmpty(request.Username) && request.Username != player.Username)
        {
            var existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.BrandId == player.BrandId && p.Username == request.Username && p.Id != playerId);

            if (existingPlayer != null)
            {
                throw new InvalidOperationException($"Username '{request.Username}' already exists in brand '{player.Brand.Code}'");
            }

            changes["Username"] = new { Old = player.Username, New = request.Username };
            player.Username = request.Username;
        }

        // Cambio de email
        if (request.Email != player.Email)
        {
            changes["Email"] = new { Old = player.Email, New = request.Email };
            player.Email = request.Email;
        }

        // Cambio de status
        if (request.Status.HasValue && request.Status.Value != player.Status)
        {
            changes["Status"] = new { Old = player.Status, New = request.Status.Value };
            player.Status = request.Status.Value;
        }

        if (changes.Any())
        {
            await _context.SaveChangesAsync();

            await _auditService.LogBackofficeActionAsync(
                currentUserId,
                "UPDATE_PLAYER",
                "Player",
                player.Id.ToString(),
                changes);

            _logger.LogInformation("Player updated: {PlayerId} by user {UserId}",
                player.Id, currentUserId);
        }

        return new GetPlayerResponse(
            player.Id,
            player.BrandId,
            player.Brand.Code,
            player.Brand.Name,
            player.Username,
            player.Email,
            player.ExternalId,
            player.Status,
            player.Wallet?.BalanceBigint ?? 0,
            player.CreatedAt);
    }

    public async Task<WalletAdjustmentResponse> AdjustPlayerWalletAsync(Guid playerId, AdjustPlayerWalletRequest request, Guid currentUserId, Guid? operatorScope = null, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por operador
        if (operatorScope.HasValue)
        {
            query = query.Where(p => p.Brand.OperatorId == operatorScope.Value);
        }

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
        {
            return new WalletAdjustmentResponse(false, 0, 0, "Player not found or access denied");
        }

        try
        {
            var externalRef = $"admin_adjust_{currentUserId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

            if (request.Amount > 0)
            {
                // Crédito
                var creditRequest = new CreditRequest(
                    playerId,
                    request.Amount,
                    LedgerReason.ADMIN_GRANT,
                    null, // no roundId
                    externalRef,
                    null, // no gameCode
                    $"ADMIN_{currentUserId}");

                var response = await _walletService.CreditAsync(creditRequest);

                if (response.Success)
                {
                    await _auditService.LogBackofficeActionAsync(
                        currentUserId,
                        "WALLET_CREDIT",
                        "Player",
                        playerId.ToString(),
                        new { 
                            Amount = request.Amount,
                            Reason = request.Reason,
                            Description = request.Description,
                            NewBalance = response.Balance,
                            ExternalRef = externalRef
                        });

                    _logger.LogInformation("Wallet credit applied: {PlayerId} - Amount: {Amount} by user {UserId}",
                        playerId, request.Amount, currentUserId);
                }

                return new WalletAdjustmentResponse(response.Success, response.Balance, response.LedgerId ?? 0, response.ErrorMessage);
            }
            else if (request.Amount < 0)
            {
                // Débito
                var debitRequest = new DebitRequest(
                    playerId,
                    Math.Abs(request.Amount),
                    LedgerReason.ADMIN_DEBIT,
                    null, // no roundId
                    externalRef,
                    null, // no gameCode
                    $"ADMIN_{currentUserId}");

                var response = await _walletService.DebitAsync(debitRequest);

                if (response.Success)
                {
                    await _auditService.LogBackofficeActionAsync(
                        currentUserId,
                        "WALLET_DEBIT",
                        "Player",
                        playerId.ToString(),
                        new { 
                            Amount = request.Amount,
                            Reason = request.Reason,
                            Description = request.Description,
                            NewBalance = response.Balance,
                            ExternalRef = externalRef
                        });

                    _logger.LogInformation("Wallet debit applied: {PlayerId} - Amount: {Amount} by user {UserId}",
                        playerId, Math.Abs(request.Amount), currentUserId);
                }

                return new WalletAdjustmentResponse(response.Success, response.Balance, response.LedgerId ?? 0, response.ErrorMessage);
            }
            else
            {
                return new WalletAdjustmentResponse(false, 0, 0, "Amount cannot be zero");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting wallet for player {PlayerId} by user {UserId}",
                playerId, currentUserId);

            return new WalletAdjustmentResponse(false, 0, 0, "Internal error occurred during wallet adjustment");
        }
    }
}