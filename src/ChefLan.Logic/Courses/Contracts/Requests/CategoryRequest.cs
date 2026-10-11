using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Requests;

/// <summary>Create and update share one shape. The slug is generated, never accepted.</summary>
public sealed record CategoryRequest(
    string Name,
    string? Description,
    string? ThumbnailUrl,
    int? DisplayOrder,
    CategoryStatus Status = CategoryStatus.Active);
