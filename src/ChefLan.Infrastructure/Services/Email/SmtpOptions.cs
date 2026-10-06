namespace ChefLan.Infrastructure.Services.Email;

/// <summary>Bound from the Smtp configuration section. Password belongs in user-secrets.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseStartTls { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = "no-reply@cheflan.local";

    public string FromDisplayName { get; set; } = "Chef Lan";
}
