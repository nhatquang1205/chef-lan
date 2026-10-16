namespace ChefLan.Logic.Abstractions;

/// <summary>
/// Port for outbound email: customer invites, password resets, receipts. Declared here so
/// use cases can depend on it; implemented in ChefLan.Infrastructure.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends now, inside the request. Throws <see cref="EmailSendException"/> when the mail could not be handed over.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// One mail. <paramref name="TextBody"/> is the plain-text alternative (some clients and spam filters want one).
/// Inline images are referenced from the HTML as <c>cid:{ContentId}</c>, so they show without the client loading
/// remote images.
/// </summary>
public sealed record EmailMessage(
    string ToEmail,
    string? ToName,
    string Subject,
    string HtmlBody,
    string TextBody,
    IReadOnlyList<EmailInlineImage>? InlineImages = null);

public sealed record EmailInlineImage(string ContentId, string FileName, string ContentType, byte[] Content);

public sealed class EmailSendException(string message, Exception? innerException = null)
    : Exception(message, innerException);
