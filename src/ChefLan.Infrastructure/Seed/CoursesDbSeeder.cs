using ChefLan.Commons.Auth;
using ChefLan.Commons.Text;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChefLan.Infrastructure.Seed;

/// <summary>
/// Idempotent: creates the first Admin staff account and the starter categories.
///
/// Seeds NO customers. Those come from registration or from staff entering a Facebook-era
/// buyer — never from a fixture.
/// </summary>
public sealed class CoursesDbSeeder(
    CoursesDbContext context,
    IPasswordHasher passwordHasher,
    ISlugGenerator slugGenerator,
    IConfiguration configuration,
    ILogger<CoursesDbSeeder> logger)
{
    private static readonly string[] StarterCategories =
    [
        "Món Bắc",
        "Món Trung",
        "Món Nam",
        "Bánh ngọt",
        "Món chay",
        "Món nước",
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminAsync(cancellationToken);
        await SeedCategoriesAsync(cancellationToken);

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

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await context.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var order = 0;

        foreach (var name in StarterCategories)
        {
            context.Categories.Add(new Category
            {
                Name = name,
                Slug = slugGenerator.Generate(name, 140),
                DisplayOrder = order += 10,
            });
        }

        logger.LogInformation("Seeded {Count} starter categories.", StarterCategories.Length);
    }
}
