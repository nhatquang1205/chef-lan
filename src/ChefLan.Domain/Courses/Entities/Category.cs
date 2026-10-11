using ChefLan.Domain.Common;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

public class Category : AuditableSoftDeletableEntity
{
    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// Storefront visibility. Separate from soft delete: InActive hides the category while
    /// keeping its slug and its course links, so it can be brought back.
    /// </summary>
    public CategoryStatus Status { get; set; } = CategoryStatus.Active;

    public ICollection<CourseCategory> CourseCategories { get; set; } = [];
}
