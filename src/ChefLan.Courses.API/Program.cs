using ChefLan.Commons.Auth;
using ChefLan.Commons.DependencyInjection;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.DB.DependencyInjection;
using ChefLan.Infrastructure.DependencyInjection;
using ChefLan.Infrastructure.Seed;
using ChefLan.Logic.DependencyInjection;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
