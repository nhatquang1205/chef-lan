using ChefLan.DB.DependencyInjection;
using ChefLan.Infrastructure.Repositories;
using ChefLan.Infrastructure.Repositories.Courses;
using ChefLan.Infrastructure.Repositories.Customers;
using ChefLan.Infrastructure.Repositories.Identity;
using ChefLan.Infrastructure.Seed;
using ChefLan.Infrastructure.Services.Email;
using ChefLan.Infrastructure.Services.Storage;
using ChefLan.Infrastructure.Services.Video;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Video;
using ChefLan.Logic.Customers.Interfaces;
using ChefLan.Logic.Identity.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.DependencyInjection;

/// <summary>
/// The outer ring's single DI entry point. Binds every port declared in ChefLan.Logic to
/// a concrete adapter, and pulls in the persistence registration from ChefLan.DB so the
/// composition root needs one call rather than two.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddChefLanInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // ChefLan.DB owns the DbContext, so it owns registering it.
        services.AddCoursesDataAccess(configuration, environment);

        services.AddRepositories();
        services.AddTechnicalServices(configuration, environment);

        services.AddScoped<CoursesDbSeeder>();

        return services;
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // identity (staff)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>();

        // customers (buyers)
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerRefreshTokenRepository, CustomerRefreshTokenRepository>();

        // courses
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ILessonVideoRepository, LessonVideoRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
    }

    private static void AddTechnicalServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<BunnyStorageOptions>(configuration.GetSection(BunnyStorageOptions.SectionName));
        services.Configure<BunnyStreamOptions>(configuration.GetSection(BunnyStreamOptions.SectionName));

        // Development logs instead of sending. Production MUST configure SMTP — a silently
        // dropped invite is indistinguishable from a broken invite flow.
        if (environment.IsDevelopment())
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }

        // Image storage (Bunny Storage) and the Stream management API. Both answer "not configured"
        // to the upload endpoints rather than failing at startup, so a deployment without them
        // still runs. Uploaded images are small and use the default timeout; video uploads can run
        // for a long time, so that client has none (the request's cancellation token ends it).
        services.AddHttpClient<IFileStorage, BunnyFileStorage>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<BunnyStorageOptions>>().Value.Endpoint.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<IVideoHost, BunnyVideoHost>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<BunnyStreamOptions>>().Value.ApiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        // Signed Bunny embeds as soon as Video:Bunny is configured; otherwise hand back the
        // stored URL so local development works with no Bunny account. The fallback is NOT
        // safe for production — an unsigned URL is a permanent, shareable link.
        var bunny = configuration.GetSection(BunnyStreamOptions.SectionName).Get<BunnyStreamOptions>();

        if (bunny?.IsConfigured == true)
        {
            services.AddSingleton<IVideoLinkResolver, BunnyVideoLinkResolver>();
        }
        else
        {
            services.AddSingleton<IVideoLinkResolver, PassthroughVideoLinkResolver>();
        }
    }
}
