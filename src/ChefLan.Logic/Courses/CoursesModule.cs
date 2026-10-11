using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChefLan.Logic.Courses;

/// <summary>
/// The only public DI entry point for the Courses domain. Services stay internal, so the
/// composition root can depend on interfaces and nothing else.
/// </summary>
public static class CoursesModule
{
    public static IServiceCollection AddCoursesModule(this IServiceCollection services)
    {
        services.AddScoped<ICourseCatalogService, CourseCatalogService>();
        services.AddScoped<ICategoryCatalogService, CategoryCatalogService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ILessonVideoService, LessonVideoService>();

        // IVideoLinkResolver is a PORT — ChefLan.Infrastructure picks the implementation.

        services.AddValidatorsFromAssemblyContaining<ICourseService>(
            includeInternalTypes: true,
            filter: f => f.ValidatorType.Namespace == typeof(CoursesModule).Namespace + ".Validators");

        return services;
    }
}
