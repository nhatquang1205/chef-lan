using System.Net.Http.Json;
using ChefLan.Logic.Courses.Contracts;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Notifications;

/// <summary>Posts to a Discord webhook. A failure is a warning, not an error: an error would be posted to the same webhook by the log alert.</summary>
internal sealed class DiscordOrderNotifier(
    HttpClient http,
    IOptions<DiscordOptions> options,
    ILogger<DiscordOrderNotifier> logger) : IOrderNotifier
{
    public async Task ContactOrderCreatedAsync(ContactOrderNotice notice, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var content = ContactOrderMessage.Build(notice, settings.AdminBaseUrl);

        try
        {
            using var response = await http.PostAsJsonAsync(
                settings.WebhookUrl,
                new { content, allowed_mentions = new { parse = Array.Empty<string>() } },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Discord answered {Status} for contact order {OrderCode}.", (int)response.StatusCode, notice.OrderCode);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Discord could not be reached for contact order {OrderCode}: {Reason}", notice.OrderCode, exception.GetType().Name);
        }
    }
}
