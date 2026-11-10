namespace ChefLan.Logic.Customers.Contracts;

public sealed record CustomerRegisterRequest(string Email, string Password, string FullName, string? Phone);

public sealed record CustomerLoginRequest(string Email, string Password);

public sealed record CustomerRefreshRequest(string RefreshToken);

public sealed record CustomerLogoutRequest(string RefreshToken);

/// <summary><c>ProfileComplete</c> is true once both phone and province are set; the website gates on it.</summary>
public sealed record CustomerResponse(
    long Id,
    string? Email,
    string FullName,
    string? Phone,
    string? ProvinceCode,
    bool ProfileComplete);

public sealed record UpdateProfileRequest(string FullName, string Phone, string ProvinceCode);

public sealed record CustomerAuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    CustomerResponse Customer);
