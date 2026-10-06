using ChefLan.Domain.Common;

namespace ChefLan.Domain.Courses.Entities;

public class Category : AuditableSoftDeletableEntity
{
    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<CourseCategory> CourseCategories { get; set; } = [];
}
