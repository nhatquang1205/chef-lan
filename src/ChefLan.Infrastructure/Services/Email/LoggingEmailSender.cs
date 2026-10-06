using ChefLan.Logic.Abstractions;
using Microsoft.Extensions.Logging;

namespace ChefLan.Infrastructure.Services.Email;

/// <summary>
/// The development default: writes the message to the log instead of sending it, so invite
/// and reset flows can be exercised without an SMTP account. Working code, not a stub —
/// but it is NOT safe for production, where a silently dropped invite looks like a bug in
/// the invite flow. <see cref="SmtpEmailSender"/> replaces it there.
/// </summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[email not sent - LoggingEmailSender] to={To} subject={Subject} bodyLength={Length}",
            toEmail,
            subject,
            htmlBody.Length);

        return Task.CompletedTask;
    }
}
