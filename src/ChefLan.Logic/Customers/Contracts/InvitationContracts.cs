namespace ChefLan.Logic.Customers.Contracts;

public sealed record InvitationTokenRequest(string Token);

public sealed record AcceptInvitationRequest(string Token, string Password);

/// <summary>What the activation page shows before the password is set.</summary>
public sealed record InvitationInfoResponse(string Email, string FullName);
