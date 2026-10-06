namespace ChefLan.Logic.Abstractions;

/// <summary>
/// Port for outbound email: customer invites, password resets, receipts. Declared here so
/// use cases can depend on it; implemented in ChefLan.Infrastructure.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
