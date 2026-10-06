using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChefLan.DB.Contexts;

/// <summary>
/// Lets `dotnet ef` build the model without booting the API. Reads CHEFLAN_DB_CONNECTION,
/// falling back to the local docker-compose database.
/// </summary>
public sealed class CoursesDbContextFactory : IDesignTimeDbContextFactory<CoursesDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5432;Database=cheflan;Username=cheflan;Password=cheflan_dev_only";

    public CoursesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CHEFLAN_DB_CONNECTION") ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                ChefLanSchemas.MigrationsHistoryTable,
                ChefLanSchemas.Courses))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CoursesDbContext(options);
    }
}
