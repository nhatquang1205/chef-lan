using ChefLan.Commons.Auth;
using Microsoft.OpenApi;

namespace ChefLan.Courses.API.Infrastructure;

/// <summary>
/// Publishes BOTH bearer schemes, so the API explorer can hold a staff token and a
/// customer token at the same time — which is how you actually test two populations.
/// </summary>
public static class OpenApiConfiguration
{
    public static IServiceCollection AddChefLanOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "ChefLan Courses API",
                    Version = "v1",
                    Description =
                        "Online cooking courses. Two separate auth flows: Internal (staff, "
                        + "/api/v1/admin/**) and Customer (buyers, everything else).",
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes[ChefLanAuthSchemes.Internal] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Staff token from POST /api/v1/admin/auth/login.",
                };

                document.Components.SecuritySchemes[ChefLanAuthSchemes.Customer] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Customer token from POST /api/v1/auth/login.",
                };

                return Task.CompletedTask;
            }));

        return services;
    }
}
