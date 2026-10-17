namespace ChefLan.Logic.Courses.Contracts.Responses;

/// <summary>The public lesson page. Videos carry no links; each one is played through the playback endpoint.</summary>
public sealed record LessonPageResponse
{
    public required string CourseSlug { get; init; }

    public required string CourseTitle { get; init; }

    public required LessonOutlineResponse Lesson { get; init; }

    /// <summary>True when the signed-in customer owns this lesson (through the course or on its own).</summary>
    public required bool HasAccess { get; init; }

    /// <summary>The viewer is signed in with a staff account: full access, but they do not "own" anything.</summary>
    public bool ViewerIsStaff { get; init; }

    /// <summary>Markdown. Null when the lesson has none, or when the viewer has no access.</summary>
    public string? Recipes { get; init; }

    /// <summary>The lesson has a recipe that this viewer is not allowed to read.</summary>
    public required bool RecipesLocked { get; init; }

    public LessonLinkResponse? Previous { get; init; }

    public LessonLinkResponse? Next { get; init; }
}

public sealed record LessonLinkResponse(string Slug, string Title);
