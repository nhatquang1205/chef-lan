using ChefLan.Logic.Abstractions;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Email;

/// <summary>
/// SKELETON — not implemented yet.
///
/// When implementing: use MailKit rather than the obsolete System.Net.Mail.SmtpClient, and
/// send through a transactional provider. Note Vietnamese subject lines need UTF-8 encoded
/// headers, which some providers get wrong — test with a real "Khóa học" subject.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("SmtpEmailSender — not wired up yet; LoggingEmailSender is the default.");
}
