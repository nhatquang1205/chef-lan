using ChefLan.Commons.Results;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Infrastructure;

/// <summary>
/// The single place an application-layer <see cref="Error"/> becomes an HTTP status code.
/// Controllers never choose status codes for failures themselves.
/// </summary>
public static class ResultToActionResultExtensions
{
    public static ActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : Problem(result.Error, controller);

    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : Problem(result.Error, controller);

    public static ActionResult<T> ToCreatedResult<T>(
        this Result<T> result,
        ControllerBase controller,
        string routeName,
        object routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtRoute(routeName, routeValues, result.Value)
            : Problem(result.Error, controller);

    private static ObjectResult Problem(Error error, ControllerBase controller)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Not available"),
            ErrorType.Upstream => (StatusCodes.Status502BadGateway, "Upstream service failed"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = error.Message,
            Instance = controller.HttpContext.Request.Path,
        };

        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = status };
    }
}
