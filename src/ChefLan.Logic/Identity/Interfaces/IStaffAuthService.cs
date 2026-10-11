using ChefLan.Commons.Results;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Identity.Contracts;

namespace ChefLan.Logic.Identity.Interfaces;

public interface IStaffAuthService
{
    Task<Result<StaffAuthResponse>> LoginAsync(
        StaffLoginRequest request, ClientInfo client, CancellationToken cancellationToken = default);

    Task<Result<StaffAuthResponse>> RefreshAsync(
        StaffRefreshRequest request, ClientInfo client, CancellationToken cancellationToken = default);

    /// <summary>Idempotent: succeeds even if the token is unknown or already revoked.</summary>
    Task<Result> LogoutAsync(
        long userId, StaffLogoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears must_change_password and revokes every refresh token, so all sessions —
    /// including the caller's — must log in again.
    /// </summary>
    Task<Result> ChangePasswordAsync(
        long userId, StaffChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result<StaffResponse>> GetMeAsync(long userId, CancellationToken cancellationToken = default);
}
