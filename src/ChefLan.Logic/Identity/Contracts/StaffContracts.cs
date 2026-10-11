namespace ChefLan.Logic.Identity.Contracts;

public sealed record StaffLoginRequest(string Email, string Password);

public sealed record StaffRefreshRequest(string RefreshToken);

public sealed record StaffLogoutRequest(string RefreshToken);

public sealed record StaffChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record StaffResponse(
    long Id,
    string Email,
    string DisplayName,
    string Role,
    string? JobTitle,
    bool MustChangePassword);

public sealed record StaffAuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    StaffResponse User);
