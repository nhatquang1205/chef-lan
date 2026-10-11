using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.DB.Contexts;

/// <summary>
/// Owns three schemas: identity (staff), customers, courses. When a second context arrives
/// (RestaurantsDbContext) it gets its own schema and its own migrations history table.
/// </summary>
public sealed class CoursesDbContext(DbContextOptions<CoursesDbContext> options) : DbContext(options)
{
    // identity
    public DbSet<User> Users => Set<User>();

    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

    // customers
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerRefreshToken> CustomerRefreshTokens => Set<CustomerRefreshToken>();

    // courses
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<LessonVideo> LessonVideos => Set<LessonVideo>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ChefLanSchemas.Courses);

        // Case-insensitive email comparison without lower() in every lookup.
        modelBuilder.HasPostgresExtension("citext");

        // Every entity is configured by its own IEntityTypeConfiguration in Configurations/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoursesDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // VND has no minor unit.
        configurationBuilder.Properties<decimal>().HavePrecision(12, 0);

        // UTC only, everywhere.
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamptz");

        base.ConfigureConventions(configurationBuilder);
    }
}
