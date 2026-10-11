using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Mapping;

public static class LessonMappings
{
    public static LessonAdminListItemResponse ToListItemResponse(this Lesson lesson) => new()
    {
        Id = lesson.Id,
        CourseId = lesson.CourseId,
        Slug = lesson.Slug,
        Title = lesson.Title,
        SortOrder = lesson.SortOrder,
        IsPublished = lesson.IsPublished,
        PriceVnd = lesson.PriceVnd,
        VideoCount = lesson.VideoCount,
        DurationSeconds = lesson.DurationSeconds,
    };

    public static LessonAdminDetailResponse ToDetailResponse(this Lesson lesson) => new()
    {
        Id = lesson.Id,
        CourseId = lesson.CourseId,
        Slug = lesson.Slug,
        Title = lesson.Title,
        Description = lesson.Description,
        Recipes = lesson.Recipes,
        ResourcesUrl = lesson.ResourcesUrl,
        SortOrder = lesson.SortOrder,
        IsPublished = lesson.IsPublished,
        PriceVnd = lesson.PriceVnd,
        VideoCount = lesson.VideoCount,
        DurationSeconds = lesson.DurationSeconds,
    };

    public static LessonVideoAdminResponse ToAdminResponse(this LessonVideo video) => new()
    {
        Id = video.Id,
        LessonId = video.LessonId,
        PublicId = video.PublicId,
        SortOrder = video.SortOrder,
        Title = video.Title,
        Description = video.Description,
        Provider = video.Provider,
        ExternalId = video.ExternalId,
        Url = video.Url,
        ThumbnailUrl = video.ThumbnailUrl,
        DurationSeconds = video.DurationSeconds,
        IsFree = video.IsFree,
        FreeTillSeconds = video.FreeTillSeconds,
        IsPublished = video.IsPublished,
    };
}
