using ChefLan.Domain.Common;

namespace ChefLan.Domain.Courses.Entities;

public class Lesson : AuditableSoftDeletableEntity, IHasRowVersion
{
    public long CourseId { get; set; }

    /// <summary>Unique within the course, not globally.</summary>
    public required string Slug { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Order within the course. Uniqueness is owned by the reorder use case, not by a
    /// unique index — a partial unique index cannot be deferred, which breaks reordering.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>Markdown. Ingredients and steps as chef Lan writes them.</summary>
    public string? Recipes { get; set; }

    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// True: part of the course price, so a course enrollment unlocks it. False: an add-on, sold only
    /// on its own (it then requires <see cref="PriceVnd"/>) and unlocked only by a <see cref="LessonEnrollment"/>.
    /// </summary>
    public bool IncludedInCourse { get; set; } = true;

    /// <summary>
    /// Price to buy THIS lesson on its own, in VND. Null means the lesson is not sold separately — it is
    /// only available through the course. Owning the course always includes every lesson; owning a
    /// lesson includes only that lesson (see <see cref="LessonEnrollment"/>).
    /// </summary>
    public decimal? PriceVnd { get; set; }

    /// <summary>Recipe PDF or ingredient list.</summary>
    public string? ResourcesUrl { get; set; }

    /// <summary>
    /// Denormalized: number of published, non-deleted videos. Maintained by the lesson
    /// video use cases inside the same transaction as the write.
    /// </summary>
    public int VideoCount { get; set; }

    /// <summary>
    /// Denormalized: sum of <see cref="LessonVideo.DurationSeconds"/> over published,
    /// non-deleted videos. Derived — never set it directly from a request.
    /// </summary>
    public int DurationSeconds { get; set; }

    public uint RowVersion { get; set; }

    public Course? Course { get; set; }

    /// <summary>
    /// A lesson holds one or more videos. The video attributes — provider, external id,
    /// duration, and whether it is free — live on the video, not here.
    /// </summary>
    public ICollection<LessonVideo> Videos { get; set; } = [];
}
