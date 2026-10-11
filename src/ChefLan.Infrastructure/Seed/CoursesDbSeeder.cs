using ChefLan.Commons.Auth;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Enums;
using ChefLan.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChefLan.Infrastructure.Seed;

/// <summary>
/// Idempotent: creates the first Admin staff account and nothing else. No categories, no
/// customers — those come from the admin endpoints and from registration.
/// </summary>
public sealed class CoursesDbSeeder(
    CoursesDbContext context,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<CoursesDbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminAsync(cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        var email = configuration["Seed:AdminEmail"] ?? "admin@cheflan.local";
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No Seed:AdminPassword configured, so no Admin account was created. Set it with "
                + "dotnet user-secrets and restart.");
            return;
        }

        context.Users.Add(new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            DisplayName = configuration["Seed:AdminDisplayName"] ?? "Chef Lan",
            Role = UserRole.Admin,
            JobTitle = "Owner",
            IsActive = true,
            MustChangePassword = true,
        });

        logger.LogInformation("Seeded the first Admin staff account {Email}.", email);
    }
}
