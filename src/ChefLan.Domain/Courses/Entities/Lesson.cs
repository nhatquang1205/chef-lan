using ChefLan.Domain.Common;
using ChefLan.Domain.Enums;

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

    public VideoProvider VideoProvider { get; set; } = VideoProvider.YouTube;

    public string? VideoUrl { get; set; }

    /// <summary>Provider id, so the frontend can build an embed without parsing URLs.</summary>
    public string? VideoExternalId { get; set; }

    public int DurationSeconds { get; set; }

    /// <summary>Playable without an enrollment.</summary>
    public bool IsFreePreview { get; set; }

    public bool IsPublished { get; set; } = true;

    public string? ThumbnailUrl { get; set; }

    /// <summary>Recipe PDF or ingredient list.</summary>
    public string? ResourcesUrl { get; set; }

    public uint RowVersion { get; set; }

    public Course? Course { get; set; }
}
