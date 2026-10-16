using ChefLan.Commons.Results;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Customers.Contracts;

namespace ChefLan.Logic.Customers.Interfaces;

public interface ICustomerAuthService
{
    Task<Result<CustomerAuthResponse>> RegisterAsync(
        CustomerRegisterRequest request, ClientInfo client, CancellationToken cancellationToken = default);

    Task<Result<CustomerAuthResponse>> LoginAsync(
        CustomerLoginRequest request, ClientInfo client, CancellationToken cancellationToken = default);

    Task<Result<CustomerAuthResponse>> RefreshAsync(
        CustomerRefreshRequest request, ClientInfo client, CancellationToken cancellationToken = default);

    /// <summary>Idempotent: succeeds even if the token is unknown or already revoked.</summary>
    Task<Result> LogoutAsync(
        long customerId, CustomerLogoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Who an invitation link is for, so the activation page can greet them. 400 for any bad link.</summary>
    Task<Result<InvitationInfoResponse>> InspectInvitationAsync(
        InvitationTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets the password from an invitation link, activates the account and signs the customer in.</summary>
    Task<Result<CustomerAuthResponse>> AcceptInvitationAsync(
        AcceptInvitationRequest request, ClientInfo client, CancellationToken cancellationToken = default);

    Task<Result<CustomerResponse>> GetMeAsync(long customerId, CancellationToken cancellationToken = default);
}
