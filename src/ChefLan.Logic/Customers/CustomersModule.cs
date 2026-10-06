using Microsoft.Extensions.DependencyInjection;

namespace ChefLan.Logic.Customers;

/// <summary>
/// The only public DI entry point for external CUSTOMER accounts and customer auth.
/// Kept separate from <see cref="Identity.IdentityModule"/> because the two populations
/// share nothing but a password hasher.
/// </summary>
public static class CustomersModule
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services) => services;
}
