using ChefLan.Commons.Auth;
using ChefLan.Commons.DependencyInjection;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.DB.DependencyInjection;
using ChefLan.Infrastructure.DependencyInjection;
using ChefLan.Infrastructure.Seed;
using ChefLan.Logic.DependencyInjection;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration.Json;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

const string WebCorsPolicy = "Web";

var builder = WebApplication.CreateBuilder(args);

// --- appsettings.Local.json: untracked local overrides -----------------------------
// Inserted directly after the last appsettings*.json source, which means it beats
// appsettings.{Environment}.json but is still beaten by user-secrets, environment
// variables and command-line args. That ordering matters: a server env var must always
// win, and it keeps ASPNETCORE_ENVIRONMENT=Development intact.
//
// Do NOT instead set ASPNETCORE_ENVIRONMENT=Local to pick the file up. That makes
// IsDevelopment() false, which silently turns off OpenAPI/Scalar, skips the seeder, and
// swaps the email sender for the unimplemented SMTP one.
//
// The file is gitignored. Prefer user-secrets for actual secrets — it cannot be committed
// at all. See appsettings.Local.example.json.
var configurationSources = builder.Configuration.Sources;
var lastAppSettingsIndex = -1;

for (var i = 0; i < configurationSources.Count; i++)
{
    if (configurationSources[i] is JsonConfigurationSource json &&
        json.Path?.StartsWith("appsettings.", StringComparison.OrdinalIgnoreCase) == true)
    {
        lastAppSettingsIndex = i;
    }
}

configurationSources.Insert(lastAppSettingsIndex + 1, new JsonConfigurationSource
{
    Path = "appsettings.Local.json",
    Optional = true,
    ReloadOnChange = true,
});

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

// --- commons + application + infrastructure ---------------------------------------
builder.Services.AddChefLanCommons();
builder.Services.AddChefLanLogic();
builder.Services.AddChefLanInfrastructure(builder.Configuration, builder.Environment);

// --- auth: two schemes, two populations, two signing keys -------------------------
builder.Services.AddChefLanAuth(builder.Configuration);
builder.Services.AddChefLanAuthorization();

// --- web ---------------------------------------------------------------------------
builder.Services
    .AddControllers(o => o.Filters.Add<ValidationFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// OpenAPI reads the minimal-API options, not MVC's, so without this the generated document
// would describe enums as numbers while the API actually sends strings.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddChefLanOpenApi();
builder.Services.AddChefLanRateLimiting(builder.Configuration);

// Browsers may call the API only from the configured web origins. No credentials: refresh
// tokens travel in the JSON body, not cookies. An empty list means no cross-origin access.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(WebCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .WithHeaders("Authorization", "Content-Type")
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithExposedHeaders("Retry-After")));
builder.Services.AddChefLanForwardedHeaders(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString(DbServiceCollectionExtensions.ConnectionStringName)!,
        name: "postgres");

var app = builder.Build();

app.UseChefLanForwardedHeaders();

// Form values (multipart prices, ids) are parsed with the request's culture. Left to the machine's
// locale, "1200000.50" is 120,000,050 under vi-VN (the dot is a thousands separator) and 1,200,000.5
// elsewhere: a silent, environment-dependent price bug. Pin it, and ignore Accept-Language.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = [CultureInfo.InvariantCulture],
    SupportedUICultures = [CultureInfo.InvariantCulture],
    RequestCultureProviders = [],
});
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Development always seeds. Production seeds ONLY when explicitly asked (Seed__RunOnStartup=true),
// for the first deploy: it creates the first Admin if none exists, and nothing else. Remove the
// flag afterwards. Migrations never run here: production applies the reviewed idempotent SQL
// from `make script` (calling Migrate() at startup races across replicas and needs DDL rights).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seed:RunOnStartup"))
{
    if (!app.Environment.IsDevelopment())
    {
        app.Logger.LogWarning("Seed:RunOnStartup is enabled. Remove it once the first Admin exists.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<CoursesDbSeeder>().SeedAsync();
}

app.UseCors(WebCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseChefLanRateLimitEmail();
app.UseRateLimiter();
app.UseChefLanCurrentUser();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
