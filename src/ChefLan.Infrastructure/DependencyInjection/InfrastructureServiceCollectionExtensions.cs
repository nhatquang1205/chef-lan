using ChefLan.DB.DependencyInjection;
using ChefLan.Infrastructure.Repositories;
using ChefLan.Infrastructure.Repositories.Courses;
using ChefLan.Infrastructure.Repositories.Customers;
using ChefLan.Infrastructure.Repositories.Identity;
using ChefLan.Infrastructure.Seed;
using ChefLan.Infrastructure.Services.Email;
using ChefLan.Infrastructure.Services.Images;
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
        services.AddScoped<ICustomerAccessCounter, CustomerAccessCounter>();

        // courses
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseIncludeRepository, CourseIncludeRepository>();
        services.AddScoped<ICourseImageRepository, CourseImageRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ILessonVideoRepository, LessonVideoRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ICustomerDirectory, CustomerDirectory>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
    }

    private static void AddTechnicalServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .Validate(
                o => string.IsNullOrWhiteSpace(o.Host)
                    || (!string.IsNullOrWhiteSpace(o.Username) && !string.IsNullOrWhiteSpace(o.Password) && !string.IsNullOrWhiteSpace(o.FromAddress)),
                "Smtp:Host is set, so Smtp:Username, Smtp:Password and Smtp:FromAddress are required.")
            .ValidateOnStart();
        services.Configure<B2StorageOptions>(configuration.GetSection(B2StorageOptions.SectionName));
        services.Configure<BunnyStreamOptions>(configuration.GetSection(BunnyStreamOptions.SectionName));

        // Links in mails point at the customer website. Required wherever mail is really sent.
        services.AddOptions<PublicSiteOptions>()
            .Bind(configuration.GetSection(PublicSiteOptions.SectionName))
            .Validate(
                o => string.IsNullOrWhiteSpace(configuration[$"{SmtpOptions.SectionName}:Host"])
                    || Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out _),
                "Smtp:Host is set, so Web:PublicBaseUrl (the customer website's origin) is required for links in mails.")
            .ValidateOnStart();
        services.AddSingleton<ICustomerEmails, CustomerEmails>();

        // Real sending whenever an SMTP server is configured, in any environment (so Gmail can be tested
        // locally). Without one, mails are only logged; outside Development that logs a warning per mail,
        // because a silently dropped invite looks exactly like a broken invite flow.
        if (!string.IsNullOrWhiteSpace(configuration[$"{SmtpOptions.SectionName}:Host"]))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        // Image storage is Backblaze B2 (S3-compatible, like wish-list); Bunny is video only. Both answer
        // "not configured" to the upload endpoints instead of failing at startup, so a deployment
        // without them still runs. Video uploads can run for a long time, so that client has no timeout
        // (the request's cancellation token ends it).
        services.AddSingleton<IFileStorage, B2FileStorage>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();

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
