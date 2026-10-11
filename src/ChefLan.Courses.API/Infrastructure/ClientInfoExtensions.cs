using ChefLan.Logic.Abstractions;
using Microsoft.Net.Http.Headers;

namespace ChefLan.Courses.API.Infrastructure;

public static class ClientInfoExtensions
{
    public static ClientInfo GetClientInfo(this HttpContext context) => new(
        context.Connection.RemoteIpAddress?.ToString(),
        context.Request.Headers[HeaderNames.UserAgent].ToString() is { Length: > 0 } ua ? ua : null);
}
