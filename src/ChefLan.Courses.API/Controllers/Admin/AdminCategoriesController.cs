using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admins/categories")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminCategoriesController(ICategoryService categories) : ControllerBase
{
    private const string GetRouteName = "AdminGetCategory";

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryAdminResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryAdminResponse>>> List(
        [FromQuery] CategoryStatus? status,
        [FromQuery] string? q,
        CancellationToken cancellationToken) =>
        Ok(await categories.ListAsync(status, q, cancellationToken));

    [HttpGet("{id:long}", Name = GetRouteName)]
    public async Task<ActionResult<CategoryAdminResponse>> Get(long id, CancellationToken cancellationToken) =>
        (await categories.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [ProducesResponseType(typeof(CategoryAdminResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryAdminResponse>> Create(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await categories.CreateAsync(request, cancellationToken);

        return result.ToCreatedResult(this, GetRouteName, new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CategoryAdminResponse>> Update(
        long id,
        CategoryRequest request,
        CancellationToken cancellationToken) =>
        (await categories.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    /// <summary>409 while a course still uses the category — InActive is the reversible alternative.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken) =>
        (await categories.DeleteAsync(id, cancellationToken)).ToActionResult(this);
}
