using ChefLan.Commons.Results;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

internal sealed class CourseImageService(
    ICourseImageRepository images,
    ICourseRepository courses,
    IFileStorage storage,
    IUnitOfWork unitOfWork) : ICourseImageService
{
    public const int MaxImagesPerCourse = 20;

    public async Task<Result<IReadOnlyList<CourseImageResponse>>> ListAsync(
        long courseId,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var list = await images.ListForCourseAsync(courseId, cancellationToken);

        return Result.Success<IReadOnlyList<CourseImageResponse>>(list.Select(i => i.ToResponse()).ToList());
    }

    public async Task<Result<CourseImageResponse>> CreateAsync(
        long courseId,
        CourseImageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var existing = await images.ListForCourseAsync(courseId, cancellationToken);

        if (existing.Count >= MaxImagesPerCourse)
        {
            return CourseErrors.TooManyImages(MaxImagesPerCourse);
        }

        var image = new CourseImage
        {
            CourseId = courseId,
            Url = request.Url.Trim(),
            Alt = SlugAllocator.Clean(request.Alt),
            Width = request.Width,
            Height = request.Height,
            SortOrder = existing.Count == 0 ? 0 : existing.Max(i => i.SortOrder) + 1,
        };

        images.Add(image);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return image.ToResponse();
    }

    public async Task<Result<CourseImageResponse>> UpdateAsync(
        long imageId,
        CourseImageUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var image = await images.GetByIdAsync(imageId, cancellationToken);

        if (image is null)
        {
            return CourseErrors.ImageNotFound(imageId);
        }

        image.Alt = SlugAllocator.Clean(request.Alt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return image.ToResponse();
    }

    public async Task<Result> DeleteAsync(long imageId, CancellationToken cancellationToken = default)
    {
        var image = await images.GetByIdAsync(imageId, cancellationToken);

        if (image is null)
        {
            return Result.Failure(CourseErrors.ImageNotFound(imageId));
        }

        var url = image.Url;

        images.Remove(image);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Best effort and after the row is gone: a failed file delete must not resurrect the image.
        await storage.DeleteByUrlAsync(url);

        return Result.Success();
    }

    public async Task<Result> ReorderAsync(
        long courseId,
        CourseImageOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return Result.Failure(CourseErrors.NotFound(courseId));
        }

        var all = await images.ListForCourseAsync(courseId, cancellationToken);

        if (!Counters.IsExactOrdering(request.ImageIds, all.Select(i => i.Id).ToList()))
        {
            return Result.Failure(LessonErrors.OrderMismatch);
        }

        var byId = all.ToDictionary(i => i.Id);

        for (var position = 0; position < request.ImageIds.Count; position++)
        {
            byId[request.ImageIds[position]].SortOrder = position;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
