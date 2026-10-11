using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ChefLan.Commons.Auth;

public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public const string ItemKey = "ChefLan.CurrentUser";

    public Task InvokeAsync(HttpContext context)
    {
        context.Items[ItemKey] = new CurrentUser(context.User);
        return next(context);
    }
}

public static class CurrentUserApplicationBuilderExtensions
{
    /// <summary>
    /// Must run AFTER UseAuthorization(). The default scheme is Customer and every policy
    /// pins its own scheme, so HttpContext.User is only the right principal once
    /// authorization has run.
    /// </summary>
    public static IApplicationBuilder UseChefLanCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserMiddleware>();
}
