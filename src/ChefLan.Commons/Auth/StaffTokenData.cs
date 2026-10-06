namespace ChefLan.Commons.Auth;

public sealed record StaffTokenData(long UserId, string Email, string DisplayName, string Role);
