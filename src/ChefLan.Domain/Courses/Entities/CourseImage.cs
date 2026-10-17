using ChefLan.Domain.Common;

namespace ChefLan.Domain.Courses.Entities;

/// <summary>
/// One picture of a course's gallery (the carousel on the course page). The first by
/// <see cref="SortOrder"/> is also the course card image. Not soft-deletable: deleting a row also
/// removes the stored file.
/// </summary>
public class CourseImage : AuditableEntity
{
    public long CourseId { get; set; }

    public required string Url { get; set; }

    public string? Alt { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    /// <summary>Uniqueness is owned by the reorder use case, not by an index.</summary>
    public int SortOrder { get; set; }

    public Course? Course { get; set; }
}
