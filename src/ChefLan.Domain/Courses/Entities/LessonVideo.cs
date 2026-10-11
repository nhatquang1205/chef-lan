using ChefLan.Domain.Common;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

/// <summary>
/// One video within a lesson. A lesson may hold several — "Phần 1 — Nhào bột",
/// "Phần 2 — Nướng" — so provider, external id, duration and the free-access flags belong
/// here rather than on the lesson.
/// </summary>
public class LessonVideo : AuditableSoftDeletableEntity, IHasRowVersion
{
    public long LessonId { get; set; }

    /// <summary>
    /// Order within the lesson. As with lessons, uniqueness is owned by the reorder use
    /// case and deliberately not by a unique index.
    /// </summary>
    public int SortOrder { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public VideoProvider Provider { get; set; } = VideoProvider.Bunny;

    /// <summary>Provider id — the Bunny video GUID. Never handed out unsigned.</summary>
    public string? ExternalId { get; set; }

    /// <summary>For providers addressed by URL rather than id (YouTube, legacy Facebook).</summary>
    public string? Url { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int DurationSeconds { get; set; }

    /// <summary>May someone without an enrollment watch this video at all?</summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// How much of a free video is free. NULL means the whole thing; a value means the
    /// first N seconds.
    ///
    /// Enforced by the FRONTEND, not here — the API still returns a URL for the full
    /// video, so seeking past the limit or reading the URL out of the page defeats it.
    /// Treat this as a teaser limit. Anything genuinely paid must have
    /// <see cref="IsFree"/> false.
    /// </summary>
    public int? FreeTillSeconds { get; set; }

    public bool IsPublished { get; set; } = true;

    public uint RowVersion { get; set; }

    public Lesson? Lesson { get; set; }
}
