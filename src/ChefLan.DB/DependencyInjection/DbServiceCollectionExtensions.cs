using ChefLan.DB.Contexts;
using ChefLan.DB.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChefLan.DB.DependencyInjection;

public static class DbServiceCollectionExtensions
{
    public const string ConnectionStringName = "ChefLanDb";

    /// <summary>
    /// Registers the DbContext, its interceptors and nothing else. This project is the
    /// persistence model only — repositories and other adapters live in
    /// ChefLan.Infrastructure, which calls this method.
    /// </summary>
    public static IServiceCollection AddCoursesDataAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing ConnectionStrings:{ConnectionStringName}.");

        services.AddScoped<AuditingSaveChangesInterceptor>();
        services.AddScoped<SoftDeleteSaveChangesInterceptor>();

        services.AddDbContext<CoursesDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable(
                        ChefLanSchemas.MigrationsHistoryTable,
                        ChefLanSchemas.Courses);
                    npgsql.EnableRetryOnFailure(3);
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditingSaveChangesInterceptor>(),
                    serviceProvider.GetRequiredService<SoftDeleteSaveChangesInterceptor>());

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }
}
