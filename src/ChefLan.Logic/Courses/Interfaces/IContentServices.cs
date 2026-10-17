using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ICategoryCatalogService
{
    /// <summary>The storefront list: Active categories only, by display order.</summary>
    Task<IReadOnlyList<CategoryResponse>> ListActiveAsync(CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryAdminResponse>> ListAsync(
        CategoryStatus? status, string? search, CancellationToken cancellationToken = default);

    Task<Result<CategoryAdminResponse>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<CategoryAdminResponse>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<CategoryAdminResponse>> UpdateAsync(
        long id, CategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public interface ICourseService
{
    Task<PagedResult<CourseListItemResponse>> ListAsync(
        AdminCourseListQuery query, CancellationToken cancellationToken = default);

    Task<Result<CourseAdminDetailResponse>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<CourseAdminDetailResponse>> CreateAsync(CourseRequest request, CancellationToken cancellationToken = default);

    Task<Result<CourseAdminDetailResponse>> UpdateAsync(
        long id, CourseRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<Result> PublishAsync(long id, CancellationToken cancellationToken = default);

    Task<Result> UnpublishAsync(long id, CancellationToken cancellationToken = default);
}

public interface ILessonService
{
    Task<Result<IReadOnlyList<LessonAdminListItemResponse>>> ListAsync(
        long courseId, CancellationToken cancellationToken = default);

    Task<Result<LessonAdminDetailResponse>> CreateAsync(
        long courseId, LessonRequest request, CancellationToken cancellationToken = default);

    Task<Result<LessonAdminDetailResponse>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<LessonAdminDetailResponse>> UpdateAsync(
        long id, LessonRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<Result> ReorderAsync(
        long courseId, LessonOrderRequest request, CancellationToken cancellationToken = default);
}

public interface ILessonVideoService
{
    Task<Result<IReadOnlyList<LessonVideoAdminResponse>>> ListAsync(
        long lessonId, CancellationToken cancellationToken = default);

    Task<Result<LessonVideoAdminResponse>> CreateAsync(
        long lessonId, LessonVideoRequest request, CancellationToken cancellationToken = default);

    Task<Result<LessonVideoAdminResponse>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<LessonVideoAdminResponse>> UpdateAsync(
        long id, LessonVideoRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<Result> ReorderAsync(
        long lessonId, LessonVideoOrderRequest request, CancellationToken cancellationToken = default);
}

public interface ICourseIncludeService
{
    Task<Result<IReadOnlyList<CourseIncludeResponse>>> ListAsync(
        long courseId, CancellationToken cancellationToken = default);

    /// <summary>Adds the pair at the end of the order, or updates the discount of an existing pair.</summary>
    Task<Result<CourseIncludeResponse>> UpsertAsync(
        long courseId, long includedCourseId, CourseIncludeRequest request, CancellationToken cancellationToken = default);

    Task<Result> RemoveAsync(long courseId, long includedCourseId, CancellationToken cancellationToken = default);

    Task<Result> ReorderAsync(
        long courseId, CourseIncludeOrderRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CourseIncludedInResponse>>> ListIncludedInAsync(
        long courseId, CancellationToken cancellationToken = default);
}

public interface ICourseImageService
{
    Task<Result<IReadOnlyList<CourseImageResponse>>> ListAsync(
        long courseId, CancellationToken cancellationToken = default);

    /// <summary>Appends the image last; at most 20 per course.</summary>
    Task<Result<CourseImageResponse>> CreateAsync(
        long courseId, CourseImageRequest request, CancellationToken cancellationToken = default);

    Task<Result<CourseImageResponse>> UpdateAsync(
        long imageId, CourseImageUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the row, then the stored file (best effort, our URLs only).</summary>
    Task<Result> DeleteAsync(long imageId, CancellationToken cancellationToken = default);

    Task<Result> ReorderAsync(
        long courseId, CourseImageOrderRequest request, CancellationToken cancellationToken = default);
}
