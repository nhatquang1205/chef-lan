using ChefLan.Logic.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ChefLan.Infrastructure.Services.Email;

/// <summary>
/// Sends through an SMTP server with MailKit. Built for Gmail with an app password (smtp.gmail.com:587,
/// STARTTLS), but nothing here is Gmail-specific. One connection per mail: invites are rare, so pooling is not
/// worth its failure modes. MimeKit encodes non-ASCII headers, so Vietnamese subjects arrive intact.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mime = Build(message);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        using var client = new SmtpClient { Timeout = (int)Timeout.TotalMilliseconds };

        try
        {
            var security = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
            await client.ConnectAsync(_options.Host, _options.Port, security, timeout.Token);
            await client.AuthenticateAsync(_options.Username, _options.Password, timeout.Token);
            await client.SendAsync(mime, timeout.Token);
            await client.DisconnectAsync(quit: true, timeout.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Never log the message body: it may hold a one-time token.
            logger.LogError(ex, "Sending email to {To} ({Subject}) failed.", message.ToEmail, message.Subject);
            throw new EmailSendException("The email could not be sent.", ex);
        }

        logger.LogInformation("Sent email to {To} ({Subject}).", message.ToEmail, message.Subject);
    }

    private MimeMessage Build(EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromDisplayName, _options.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName ?? string.Empty, message.ToEmail));
        mime.Subject = message.Subject;

        var body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody };

        foreach (var image in message.InlineImages ?? [])
        {
            var resource = body.LinkedResources.Add(image.FileName, image.Content, ContentType.Parse(image.ContentType));
            // RFC 2392: the Content-Id is an addr-spec (local@domain); clients resolve the HTML's cid: through it alone.
            resource.ContentId = image.ContentId;
            resource.ContentLocation = null;
        }

        mime.Body = body.ToMessageBody();
        return mime;
    }
}
