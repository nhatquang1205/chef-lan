using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Mapping;

public static class CourseImageMappings
{
    public static CourseImageResponse ToResponse(this CourseImage image) => new()
    {
        Id = image.Id,
        Url = image.Url,
        Alt = image.Alt,
        Width = image.Width,
        Height = image.Height,
        SortOrder = image.SortOrder,
    };

    public static CourseImagePublicResponse ToPublicResponse(this CourseImage image) =>
        new(image.Url, image.Alt, image.Width, image.Height);

    /// <summary>The card image: the first gallery image by order, else the cover.</summary>
    public static string? CardImageUrl(this Course course) =>
        course.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => i.Url).FirstOrDefault()
        ?? course.ThumbnailUrl;
}
