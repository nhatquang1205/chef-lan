using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Video;

namespace ChefLan.Infrastructure.Services.Video;

/// <summary>
/// v1: hands back the stored URL as-is. Replace this — do not edit callers — when link
/// leaking becomes a real problem. See <see cref="IVideoLinkResolver"/>.
/// </summary>
public sealed class PassthroughVideoLinkResolver : IVideoLinkResolver
{
    public ValueTask<string?> ResolveAsync(
        Lesson lesson,
        long customerId,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(lesson.VideoUrl);
}
