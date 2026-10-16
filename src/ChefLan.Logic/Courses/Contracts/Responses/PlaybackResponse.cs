namespace ChefLan.Logic.Courses.Contracts.Responses;

/// <summary>What the watch page needs to play one video.</summary>
public sealed record PlaybackResponse
{
    public required string VideoId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    /// <summary>Signed, short-lived link to the player. Never the provider id itself.</summary>
    public required string Url { get; init; }

    /// <summary>When <see cref="Url"/> stops working for a NEW page load; null when links do not expire.</summary>
    public DateTime? ExpiresAt { get; init; }

    public required bool IsFree { get; init; }

    /// <summary>
    /// Null means no limit. Always null for someone who owns the course or the lesson; otherwise the
    /// video's own limit. Enforced by the watch page, not here: a teaser limit.
    /// </summary>
    public int? FreeTillSeconds { get; init; }

    /// <summary>True when the viewer owns this lesson (via the course or the lesson itself).</summary>
    public required bool HasFullAccess { get; init; }

    public required int DurationSeconds { get; init; }

    public required string CourseSlug { get; init; }

    public required string CourseTitle { get; init; }

    public required string LessonTitle { get; init; }

    public required string LessonSlug { get; init; }
}
