using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Liveness only. Database readiness is the /health endpoint from HealthChecks.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "ChefLan.Courses.API",
        utcNow = DateTime.UtcNow,
    });
}
