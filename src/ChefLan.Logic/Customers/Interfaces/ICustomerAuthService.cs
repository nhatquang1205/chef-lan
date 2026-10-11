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

    Task<Result<CustomerResponse>> GetMeAsync(long customerId, CancellationToken cancellationToken = default);
}
