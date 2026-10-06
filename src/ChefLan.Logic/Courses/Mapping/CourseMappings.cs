using ChefLan.Commons.Money;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Mapping;

/// <summary>
/// Hand-written mapping on purpose: debuggable, greppable, free, and faster than a
/// reflection-based mapper. AutoMapper went commercial in 2025.
/// </summary>
public static class CourseMappings
{
    public static CourseListItemResponse ToListItemResponse(this Course course) => new()
    {
        Id = course.Id,
        Slug = course.Slug,
        Title = course.Title,
        Subtitle = course.Subtitle,
        ThumbnailUrl = course.ThumbnailUrl,
        Level = course.Level,
        Status = course.Status,
        PublishedAt = course.PublishedAt,
        PriceVnd = course.PriceVnd,
        CompareAtPriceVnd = course.CompareAtPriceVnd,
        PriceDisplay = Vnd.ToDisplay(course.PriceVnd),
        LessonCount = course.LessonCount,
        TotalDurationSeconds = course.TotalDurationSeconds,
        AccessDurationDays = course.AccessDurationDays,
        Categories = course.CourseCategories
            .Where(cc => cc.Category is not null)
            .Select(cc => cc.Category!.ToResponse())
            .OrderBy(c => c.DisplayOrder)
            .ToList(),
    };

    public static CategoryResponse ToResponse(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Slug = category.Slug,
        ThumbnailUrl = category.ThumbnailUrl,
        DisplayOrder = category.DisplayOrder,
    };
}
