using Casino.Application.DTOs.Admin;

namespace Casino.Application.Services;

public interface IPasswordService
{
    Task<PasswordChangeResponse> ChangeUserPasswordAsync(Guid userId, ChangePasswordRequest request, Guid currentUserId, Guid? brandScope = null);
    Task<PasswordChangeResponse> ResetUserPasswordAsync(Guid userId, ResetPasswordRequest request, Guid currentUserId, Guid? brandScope = null);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}