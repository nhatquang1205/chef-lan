using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Mapping;

public static class CourseIncludeMappings
{
    public static CourseIncludeResponse ToResponse(this CourseInclude include) => new()
    {
        IncludedCourseId = include.IncludedCourseId,
        Slug = include.IncludedCourse!.Slug,
        Title = include.IncludedCourse.Title,
        Status = include.IncludedCourse.Status,
        PriceVnd = include.IncludedCourse.PriceVnd,
        LessonCount = include.IncludedCourse.LessonCount,
        DiscountVnd = include.DiscountVnd,
        SortOrder = include.SortOrder,
    };

    public static CourseIncludedInResponse ToIncludedInResponse(this Course course) => new()
    {
        Id = course.Id,
        Slug = course.Slug,
        Title = course.Title,
    };
}
