namespace ChefLan.Logic.Customers.Contracts;

public sealed record CustomerRegisterRequest(string Email, string Password, string FullName, string? Phone);

public sealed record CustomerLoginRequest(string Email, string Password);

public sealed record CustomerRefreshRequest(string RefreshToken);

public sealed record CustomerLogoutRequest(string RefreshToken);

public sealed record CustomerResponse(long Id, string? Email, string FullName, string? Phone);

public sealed record CustomerAuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    CustomerResponse Customer);
