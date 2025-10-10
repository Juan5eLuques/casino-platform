using Casino.Application.DTOs.Admin;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _passwordHasher;
    private readonly CasinoDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<PasswordService> _logger;

    public PasswordService(CasinoDbContext context, IAuditService auditService, ILogger<PasswordService> logger)
    {
        _passwordHasher = new PasswordHasher<object>();
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
            return false;

        var result = _passwordHasher.VerifyHashedPassword(new object(), hash, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    public async Task<PasswordChangeResponse> ChangeUserPasswordAsync(Guid userId, ChangePasswordRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var query = _context.BackofficeUsers.Where(u => u.Id == userId);

        // Apply brand scope if not SUPER_ADMIN
        if (brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value);
        }

        var user = await query.FirstOrDefaultAsync();

        if (user == null)
        {
            return new PasswordChangeResponse(false, "User not found or access denied");
        }

        // If not SUPER_ADMIN, verify current password
        if (currentUserId != userId && !string.IsNullOrEmpty(request.CurrentPassword))
        {
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return new PasswordChangeResponse(false, "Current password is incorrect");
            }
        }

        // Hash new password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.LastLoginAt = null; // Force re-login

        await _context.SaveChangesAsync();

        // Audit
        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "CHANGE_PASSWORD",
            "BackofficeUser",
            userId.ToString(),
            new { TargetUserId = userId, TargetUsername = user.Username, ChangedBy = currentUserId });

        _logger.LogInformation("Password changed for user {Username} by user {ChangedByUserId}", user.Username, currentUserId);

        return new PasswordChangeResponse(true, "Password changed successfully", DateTime.UtcNow);
    }

    public async Task<PasswordChangeResponse> ResetUserPasswordAsync(Guid userId, ResetPasswordRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var query = _context.BackofficeUsers.Where(u => u.Id == userId);

        // Apply brand scope if not SUPER_ADMIN
        if (brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value);
        }

        var user = await query.FirstOrDefaultAsync();

        if (user == null)
        {
            return new PasswordChangeResponse(false, "User not found or access denied");
        }

        // Hash new password
        user.PasswordHash = HashPassword(request.NewPassword);
        
        if (request.ForceChangeOnNextLogin)
        {
            user.LastLoginAt = null; // Force re-login
        }

        await _context.SaveChangesAsync();

        // Audit
        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "RESET_PASSWORD",
            "BackofficeUser",
            userId.ToString(),
            new { TargetUserId = userId, TargetUsername = user.Username, ResetBy = currentUserId, ForceChangeOnNextLogin = request.ForceChangeOnNextLogin });

        _logger.LogInformation("Password reset for user {Username} by user {ResetByUserId}", user.Username, currentUserId);

        return new PasswordChangeResponse(true, "Password reset successfully", DateTime.UtcNow);
    }
}