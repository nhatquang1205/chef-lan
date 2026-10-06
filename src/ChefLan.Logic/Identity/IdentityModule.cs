using Microsoft.Extensions.DependencyInjection;

namespace ChefLan.Logic.Identity;

/// <summary>
/// The only public DI entry point for internal STAFF accounts and staff auth.
/// Services arrive in step 2; the ports they implement are already declared.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services) => services;
}
