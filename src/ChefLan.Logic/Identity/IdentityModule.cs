using ChefLan.Logic.Identity.Interfaces;
using ChefLan.Logic.Identity.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChefLan.Logic.Identity;

/// <summary>
/// The only public DI entry point for internal STAFF accounts and staff auth.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IStaffAuthService, StaffAuthService>();
        services.AddValidatorsFromAssemblyContaining<IStaffAuthService>(
            includeInternalTypes: true,
            filter: f => f.ValidatorType.Namespace == typeof(IdentityModule).Namespace + ".Validators");

        return services;
    }
}
