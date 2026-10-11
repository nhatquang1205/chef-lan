using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record CategoryAdminResponse
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }

    public required int DisplayOrder { get; init; }

    public required CategoryStatus Status { get; init; }
}
