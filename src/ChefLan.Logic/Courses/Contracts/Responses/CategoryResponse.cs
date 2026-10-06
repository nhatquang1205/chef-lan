namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record CategoryResponse
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public string? ThumbnailUrl { get; init; }

    public required int DisplayOrder { get; init; }
}
