using ChefLan.Commons.Geography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

/// <summary>The fixed list of provinces and cities a customer can pick. Anonymous.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/provinces")]
[Produces("application/json")]
public sealed class ProvincesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Province>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<Province>> List()
    {
        Response.Headers.CacheControl = "public, max-age=86400";

        return Ok(VietnamProvinces.All);
    }
}
