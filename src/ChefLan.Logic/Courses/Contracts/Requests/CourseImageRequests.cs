namespace ChefLan.Logic.Courses.Contracts.Requests;

/// <summary>Attaches an already uploaded image (the URL the upload endpoint returned) to a course.</summary>
public sealed record CourseImageRequest(string Url, string? Alt, int? Width, int? Height);

public sealed record CourseImageUpdateRequest(string? Alt);

public sealed record CourseImageOrderRequest(IReadOnlyList<long> ImageIds);
