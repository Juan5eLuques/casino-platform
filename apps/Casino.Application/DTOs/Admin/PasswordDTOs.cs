namespace Casino.Application.DTOs.Admin;

public record ChangePasswordRequest(
    string? CurrentPassword,
    string NewPassword
);

public record ResetPasswordRequest(
    string NewPassword,
    bool ForceChangeOnNextLogin = false
);

public record PasswordChangeResponse(
    bool Success,
    string Message,
    DateTime? LastPasswordChangeAt = null
);