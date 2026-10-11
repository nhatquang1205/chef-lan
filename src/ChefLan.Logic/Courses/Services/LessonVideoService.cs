using ChefLan.Commons.Results;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

internal sealed class LessonVideoService(
    ILessonVideoRepository videos,
    ILessonRepository lessons,
    ICourseRepository courses,
    IUnitOfWork unitOfWork) : ILessonVideoService
{
    public async Task<Result<IReadOnlyList<LessonVideoAdminResponse>>> ListAsync(
        long lessonId,
        CancellationToken cancellationToken = default)
    {
        if (await lessons.GetByIdAsync(lessonId, cancellationToken) is null)
        {
            return LessonErrors.NotFound(lessonId);
        }

        var list = await videos.ListForLessonAsync(lessonId, cancellationToken);

        return Result.Success<IReadOnlyList<LessonVideoAdminResponse>>(list.Select(v => v.ToAdminResponse()).ToList());
    }

    public async Task<Result<LessonVideoAdminResponse>> CreateAsync(
        long lessonId,
        LessonVideoRequest request,
        CancellationToken cancellationToken = default)
    {
        var lesson = await lessons.GetByIdAsync(lessonId, cancellationToken);

        if (lesson is null)
        {
            return LessonErrors.NotFound(lessonId);
        }

        var existing = await videos.ListForLessonAsync(lessonId, cancellationToken);
        var video = new LessonVideo
        {
            LessonId = lessonId,
            Title = request.Title.Trim(),
            SortOrder = existing.Count == 0 ? 0 : existing.Max(v => v.SortOrder) + 1,
        };

        Apply(video, request);
        videos.Add(video);

        await RecomputeAsync(lesson, existing.Append(video), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return video.ToAdminResponse();
    }

    public async Task<Result<LessonVideoAdminResponse>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var video = await videos.GetByIdAsync(id, cancellationToken);

        return video is null ? LessonErrors.VideoNotFound(id) : video.ToAdminResponse();
    }

    public async Task<Result<LessonVideoAdminResponse>> UpdateAsync(
        long id,
        LessonVideoRequest request,
        CancellationToken cancellationToken = default)
    {
        var video = await videos.GetByIdAsync(id, cancellationToken);

        if (video is null)
        {
            return LessonErrors.VideoNotFound(id);
        }

        var lesson = await lessons.GetByIdAsync(video.LessonId, cancellationToken);

        Apply(video, request);

        if (lesson is not null)
        {
            await RecomputeAsync(lesson, await videos.ListForLessonAsync(lesson.Id, cancellationToken), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return video.ToAdminResponse();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var video = await videos.GetByIdAsync(id, cancellationToken);

        if (video is null)
        {
            return Result.Failure(LessonErrors.VideoNotFound(id));
        }

        var lesson = await lessons.GetByIdAsync(video.LessonId, cancellationToken);
        var all = await videos.ListForLessonAsync(video.LessonId, cancellationToken);

        videos.Remove(video);

        if (lesson is not null)
        {
            await RecomputeAsync(lesson, all.Where(v => v.Id != id), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReorderAsync(
        long lessonId,
        LessonVideoOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await lessons.GetByIdAsync(lessonId, cancellationToken) is null)
        {
            return Result.Failure(LessonErrors.NotFound(lessonId));
        }

        var all = await videos.ListForLessonAsync(lessonId, cancellationToken);

        if (!Counters.IsExactOrdering(request.VideoIds, all.Select(v => v.Id).ToList()))
        {
            return Result.Failure(LessonErrors.OrderMismatch);
        }

        var byId = all.ToDictionary(v => v.Id);

        for (var position = 0; position < request.VideoIds.Count; position++)
        {
            byId[request.VideoIds[position]].SortOrder = position;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>Lesson counters from its videos, then the course roll-up from its lessons.</summary>
    private async Task RecomputeAsync(
        Lesson lesson,
        IEnumerable<LessonVideo> liveVideos,
        CancellationToken cancellationToken)
    {
        Counters.Apply(lesson, liveVideos);

        var course = await courses.GetByIdAsync(lesson.CourseId, cancellationToken);

        if (course is not null)
        {
            // The same tracked instances, so this lesson's fresh totals are already in the list.
            Counters.Apply(
                course,
                await lessons.ListForCourseAsync(lesson.CourseId, includeUnpublished: true, cancellationToken));
        }
    }

    private static void Apply(LessonVideo video, LessonVideoRequest request)
    {
        video.Title = request.Title.Trim();
        video.Description = SlugAllocator.Clean(request.Description);
        video.Provider = request.Provider;
        video.ExternalId = SlugAllocator.Clean(request.ExternalId);
        video.Url = SlugAllocator.Clean(request.Url);
        video.ThumbnailUrl = SlugAllocator.Clean(request.ThumbnailUrl);
        video.DurationSeconds = request.DurationSeconds;
        video.IsFree = request.IsFree;
        video.FreeTillSeconds = request.FreeTillSeconds;
        video.IsPublished = request.IsPublished;
    }
}
