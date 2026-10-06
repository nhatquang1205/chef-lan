using ChefLan.Commons.Text;
using ChefLan.Commons.Time;
using Microsoft.Extensions.DependencyInjection;

namespace ChefLan.Commons.DependencyInjection;

public static class CommonsServiceCollectionExtensions
{
    /// <summary>
    /// Cross-cutting primitives every layer may depend on. Stateless, so singletons.
    /// Auth registrations live in <see cref="Auth.AuthServiceCollectionExtensions"/>.
    /// </summary>
    public static IServiceCollection AddChefLanCommons(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISlugGenerator, VietnameseSlugGenerator>();

        return services;
    }
}
