using ChefLan.Commons.Auth;
using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Courses.API.Contracts;
using ChefLan.Courses.API.Infrastructure;
using FluentValidation;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admins/courses")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminCoursesController(
    ICourseService courses,
    IMediaUploadService uploads,
    IValidator<CourseRequest> courseValidator) : ControllerBase
{
    private const string GetRouteName = "AdminGetCourse";

    // A thumbnail is at most 5 MB; the rest is room for the other form fields and multipart framing.
    private const int MaxFormRequestBytes = 6 * 1024 * 1024;

    /// <summary>Drafts included, unlike the public list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CourseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseListItemResponse>>> List(
        [FromQuery] CourseStatus? status,
        [FromQuery] string? q,
        [FromQuery] string? categorySlug,
        [FromQuery] CourseLevel? level,
        [FromQuery] CourseSortOrder sort = CourseSortOrder.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var query = new AdminCourseListQuery
        {
            Status = status,
            Q = q,
            CategorySlug = categorySlug,
            Level = level,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
        };

        return Ok(await courses.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}", Name = GetRouteName)]
    public async Task<ActionResult<CourseAdminDetailResponse>> Get(long id, CancellationToken cancellationToken) =>
        (await courses.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CourseAdminDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseAdminDetailResponse>> Create(
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courses.CreateAsync(request, cancellationToken);

        return result.ToCreatedResult(this, GetRouteName, new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    /// <summary>Status is not a field here — use publish and unpublish.</summary>
    [HttpPut("{id:long}")]
    [Consumes("application/json")]
    public async Task<ActionResult<CourseAdminDetailResponse>> Update(
        long id,
        CourseRequest request,
        CancellationToken cancellationToken) =>
        (await courses.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Same as the JSON create, as multipart/form-data, with the thumbnail as <c>thumbnailUrl</c> OR a
    /// <c>thumbnailFile</c> upload (not both). The form is validated before anything is uploaded, and an
    /// image whose course then fails to save is deleted again.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFormRequestBytes)]
    [ProducesResponseType(typeof(CourseAdminDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseAdminDetailResponse>> CreateForm(
        [FromForm] CourseFormRequest form,
        CancellationToken cancellationToken)
    {
        var (request, uploadedUrl, failure) = await PrepareAsync(form, cancellationToken);

        if (failure is not null)
        {
            return failure;
        }

        var result = await SaveAsync(() => courses.CreateAsync(request!, cancellationToken), uploadedUrl);

        return result.ToCreatedResult(this, GetRouteName, new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    /// <summary>
    /// The multipart twin of the JSON update. Like the JSON PUT it REPLACES the whole course: leave out
    /// both thumbnail fields and the thumbnail is cleared.
    /// </summary>
    [HttpPut("{id:long}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFormRequestBytes)]
    public async Task<ActionResult<CourseAdminDetailResponse>> UpdateForm(
        long id,
        [FromForm] CourseFormRequest form,
        CancellationToken cancellationToken)
    {
        var (request, uploadedUrl, failure) = await PrepareAsync(form, cancellationToken);

        if (failure is not null)
        {
            return failure;
        }

        return (await SaveAsync(() => courses.UpdateAsync(id, request!, cancellationToken), uploadedUrl))
            .ToActionResult(this);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken) =>
        (await courses.DeleteAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>409 unless the course has a published lesson holding a playable video.</summary>
    [HttpPost("{id:long}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Publish(long id, CancellationToken cancellationToken) =>
        (await courses.PublishAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:long}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Unpublish(long id, CancellationToken cancellationToken) =>
        (await courses.UnpublishAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Turns the form into a CourseRequest: validates the fields FIRST (so a bad form uploads nothing),
    /// then uploads the thumbnail file if there is one. Returns the request, the URL of any image it
    /// just uploaded, or the failure response.
    /// </summary>
    private async Task<(CourseRequest? Request, string? UploadedUrl, ActionResult? Failure)> PrepareAsync(
        CourseFormRequest form,
        CancellationToken cancellationToken)
    {
        var url = string.IsNullOrWhiteSpace(form.ThumbnailUrl) ? null : form.ThumbnailUrl.Trim();
        var file = form.ThumbnailFile is { Length: > 0 } f ? f : null;

        if (file is not null && url is not null)
        {
            return (null, null, Result.Failure(Error.Validation(
                "course.thumbnail_conflict",
                "Send either thumbnailUrl or thumbnailFile, not both.")).ToActionResult(this));
        }

        var request = form.ToRequest(url);
        var validation = await courseValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return (null, null, ValidationProblem(ModelState));
        }

        if (file is null)
        {
            return (request, null, null);
        }

        if (file.Length > MaxFormRequestBytes)
        {
            return (null, null, Result.Failure(Error.Validation("upload.image_too_large", "Images may be at most 5 MB."))
                .ToActionResult(this));
        }

        await using var stream = file.OpenReadStream();
        var bytes = new byte[file.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);

        var uploaded = await uploads.UploadImageAsync(bytes, cancellationToken);

        return uploaded.IsFailure
            ? (null, null, Result.Failure(uploaded.Error).ToActionResult(this))
            : (request with { ThumbnailUrl = uploaded.Value.Url }, uploaded.Value.Url, null);
    }

    /// <summary>Runs the save; if it fails (or throws) after an image was uploaded, that image is removed.</summary>
    private async Task<Result<CourseAdminDetailResponse>> SaveAsync(
        Func<Task<Result<CourseAdminDetailResponse>>> save,
        string? uploadedUrl)
    {
        Result<CourseAdminDetailResponse> result;

        try
        {
            result = await save();
        }
        catch
        {
            if (uploadedUrl is not null)
            {
                await uploads.DeleteImageAsync(uploadedUrl);
            }

            throw;
        }

        if (result.IsFailure && uploadedUrl is not null)
        {
            await uploads.DeleteImageAsync(uploadedUrl);
        }

        return result;
    }
}
