using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

/// <summary>The storefront categories. Anonymous, Active only.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/categories")]
[Produces("application/json")]
public sealed class CategoriesController(ICategoryCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await catalog.ListActiveAsync(cancellationToken));
}
