namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record CourseImageResponse
{
    public required long Id { get; init; }

    public required string Url { get; init; }

    public string? Alt { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public required int SortOrder { get; init; }
}

/// <summary>A gallery image on the public course page, in carousel order.</summary>
public sealed record CourseImagePublicResponse(string Url, string? Alt, int? Width, int? Height);
