using ChefLan.Logic.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChefLan.Infrastructure.Services.Email;

/// <summary>
/// Used when no SMTP server is configured: writes the message to the log instead of sending it, so invite
/// flows can be exercised locally. In Development the text body is logged too (it carries the invite link you
/// need); anywhere else only a warning is logged, because a body may contain a one-time token.
/// </summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger, IHostEnvironment environment) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (environment.IsDevelopment())
        {
            logger.LogInformation(
                "[email not sent: no SMTP configured] to={To} subject={Subject}\n{Body}",
                message.ToEmail,
                message.Subject,
                message.TextBody);
        }
        else
        {
            logger.LogWarning(
                "SMTP is not configured (Smtp:Host is empty): the email to {To} ({Subject}) was NOT sent.",
                message.ToEmail,
                message.Subject);
        }

        return Task.CompletedTask;
    }
}
