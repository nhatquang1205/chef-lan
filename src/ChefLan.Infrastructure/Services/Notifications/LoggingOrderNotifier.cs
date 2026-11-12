using ChefLan.Logic.Courses.Contracts;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Notifications;

/// <summary>Used when no Discord webhook is configured: the message goes to the log instead.</summary>
internal sealed class LoggingOrderNotifier(
    ILogger<LoggingOrderNotifier> logger,
    IHostEnvironment environment,
    IOptions<DiscordOptions> options) : IOrderNotifier
{
    public Task ContactOrderCreatedAsync(ContactOrderNotice notice, CancellationToken cancellationToken = default)
    {
        if (environment.IsDevelopment())
        {
            logger.LogInformation(
                "[discord not sent: no webhook configured]\n{Message}",
                ContactOrderMessage.Build(notice, options.Value.AdminBaseUrl));
        }
        else
        {
            logger.LogInformation("Contact order {OrderCode} created (Discord webhook not configured).", notice.OrderCode);
        }

        return Task.CompletedTask;
    }
}
