using Casino.Application.DTOs.Balance;
using Casino.Application.Services;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// Implementación del servicio de balance para usuario logueado
/// </summary>
public class BalanceService : IBalanceService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<BalanceService> _logger;

    public BalanceService(CasinoDbContext context, ILogger<BalanceService> logger)
    {
    _context = context;
        _logger = logger;
    }

    public async Task<UserBalanceResponse> GetMyBalanceAsync(Guid userId, string userType)
    {
        _logger.LogInformation("Getting balance for user: {UserId}, Type: {UserType}", userId, userType);

        if (userType == "BACKOFFICE")
     {
     var user = await _context.BackofficeUsers
      .Include(u => u.Brand)
      .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
          throw new InvalidOperationException($"Backoffice user {userId} not found");
            }

    return new UserBalanceResponse(
   user.Id,
  "BACKOFFICE",
        user.Username,
           user.WalletBalance,
        user.Role.ToString(),
         user.BrandId,
             user.Brand?.Name
          );
        }
        else if (userType == "PLAYER")
      {
            var player = await _context.Players
             .Include(p => p.Brand)
     .FirstOrDefaultAsync(p => p.Id == userId);

            if (player == null)
            {
 throw new InvalidOperationException($"Player {userId} not found");
       }

        return new UserBalanceResponse(
       player.Id,
   "PLAYER",
          player.Username,
           player.WalletBalance,
    null, // Players no tienen rol
                player.BrandId,
        player.Brand?.Name
   );
        }
        else
        {
            throw new InvalidOperationException($"Invalid user type: {userType}");
    }
    }
}
