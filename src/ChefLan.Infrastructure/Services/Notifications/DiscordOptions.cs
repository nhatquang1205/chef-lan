namespace ChefLan.Infrastructure.Services.Notifications;

public sealed class DiscordOptions
{
    public const string SectionName = "Notifications:Discord";

    /// <summary>Empty = messages are only logged. A credential: set it from the environment, never a committed file.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Origin of the admin site, no trailing slash. Empty = the message carries no link.</summary>
    public string AdminBaseUrl { get; set; } = string.Empty;
}
