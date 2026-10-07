using ChefLan.Commons.Auth;
using ChefLan.Commons.DependencyInjection;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.DB.DependencyInjection;
using ChefLan.Infrastructure.DependencyInjection;
using ChefLan.Infrastructure.Seed;
using ChefLan.Logic.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using Scalar.AspNetCore;
using Serilog;

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
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddChefLanOpenApi();

builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString(DbServiceCollectionExtensions.ConnectionStringName)!,
        name: "postgres");

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Development only. Production migrations ship as reviewed idempotent SQL:
    //   dotnet ef migrations script --idempotent
    // Calling Migrate() at startup races across replicas and needs DDL rights at runtime.
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<CoursesDbSeeder>().SeedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
