namespace ChefLan.Commons.Auth;

public sealed record AccessToken(string Value, DateTime ExpiresAt);
