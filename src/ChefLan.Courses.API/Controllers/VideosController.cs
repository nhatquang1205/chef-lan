using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/videos")]
[Produces("application/json")]
public sealed class VideosController(IPlaybackService playback, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// A fresh signed link plus context for the watch page. 404 for anything not public (unknown id, an
    /// unpublished video, lesson or course); 403 when the video is not free. The id is the opaque
    /// <c>publicId</c>, never the integer id.
    /// </summary>
    [HttpGet("{publicId}/playback")]
    [ProducesResponseType(typeof(PlaybackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaybackResponse>> GetPlayback(string publicId, CancellationToken cancellationToken)
    {
        long? customerId = currentUser.TryGetCustomerId(out var id) ? id : null;

        var result = await playback.GetAsync(publicId, customerId, cancellationToken);

        // A signed link must never be cached or shared by a proxy.
        Response.Headers.CacheControl = "no-store";

        return result.ToActionResult(this);
    }
}
