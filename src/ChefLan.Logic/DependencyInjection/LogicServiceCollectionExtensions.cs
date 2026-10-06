using ChefLan.Logic.Courses;
using ChefLan.Logic.Customers;
using ChefLan.Logic.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ChefLan.Logic.DependencyInjection;

public static class LogicServiceCollectionExtensions
{
    /// <summary>Registers every domain module. One line per domain, nothing else.</summary>
    public static IServiceCollection AddChefLanLogic(this IServiceCollection services) => services
        .AddIdentityModule()
        .AddCustomersModule()
        .AddCoursesModule();
}
