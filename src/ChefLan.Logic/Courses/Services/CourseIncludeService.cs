using ChefLan.Commons.Results;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

internal sealed class CourseIncludeService(
    ICourseRepository courses,
    ICourseIncludeRepository includes,
    IUnitOfWork unitOfWork) : ICourseIncludeService
{
    public async Task<Result<IReadOnlyList<CourseIncludeResponse>>> ListAsync(
        long courseId,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var all = await includes.ListForCourseAsync(courseId, cancellationToken);

        return Result.Success<IReadOnlyList<CourseIncludeResponse>>(all.Select(i => i.ToResponse()).ToList());
    }

    public async Task<Result<CourseIncludeResponse>> UpsertAsync(
        long courseId,
        long includedCourseId,
        CourseIncludeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (courseId == includedCourseId)
        {
            return CourseErrors.IncludeSelf;
        }

        var parent = await courses.GetByIdAsync(courseId, cancellationToken);

        if (parent is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var child = await courses.GetByIdAsync(includedCourseId, cancellationToken);

        if (child is null)
        {
            return CourseErrors.NotFound(includedCourseId);
        }

        if (await includes.CountIncludedInAsync(courseId, cancellationToken) > 0
            || await includes.CountIncludesAsync(includedCourseId, cancellationToken) > 0)
        {
            return CourseErrors.IncludeNested;
        }

        if (request.DiscountVnd > parent.PriceVnd)
        {
            return CourseErrors.IncludeDiscountTooHigh;
        }

        var existing = await includes.GetAsync(courseId, includedCourseId, cancellationToken);

        if (existing is not null)
        {
            existing.DiscountVnd = request.DiscountVnd;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return existing.ToResponse();
        }

        var current = await includes.ListForCourseAsync(courseId, cancellationToken);
        var created = new CourseInclude
        {
            CourseId = courseId,
            IncludedCourseId = includedCourseId,
            IncludedCourse = child,
            DiscountVnd = request.DiscountVnd,
            SortOrder = current.Count == 0 ? 0 : current.Max(i => i.SortOrder) + 1,
        };

        includes.Add(created);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return created.ToResponse();
    }

    public async Task<Result> RemoveAsync(
        long courseId,
        long includedCourseId,
        CancellationToken cancellationToken = default)
    {
        var existing = await includes.GetAsync(courseId, includedCourseId, cancellationToken);

        if (existing is null)
        {
            return Result.Failure(CourseErrors.IncludeNotFound(courseId, includedCourseId));
        }

        includes.Remove(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReorderAsync(
        long courseId,
        CourseIncludeOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return Result.Failure(CourseErrors.NotFound(courseId));
        }

        var all = await includes.ListForCourseAsync(courseId, cancellationToken);

        if (!Counters.IsExactOrdering(request.IncludedCourseIds, all.Select(i => i.IncludedCourseId).ToList()))
        {
            return Result.Failure(LessonErrors.OrderMismatch);
        }

        var byId = all.ToDictionary(i => i.IncludedCourseId);

        for (var position = 0; position < request.IncludedCourseIds.Count; position++)
        {
            byId[request.IncludedCourseIds[position]].SortOrder = position;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<CourseIncludedInResponse>>> ListIncludedInAsync(
        long courseId,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var parents = await includes.ListParentsAsync(courseId, cancellationToken);

        return Result.Success<IReadOnlyList<CourseIncludedInResponse>>(
            parents.Select(c => c.ToIncludedInResponse()).ToList());
    }
}
