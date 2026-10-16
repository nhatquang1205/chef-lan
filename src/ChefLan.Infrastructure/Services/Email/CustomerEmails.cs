using System.Globalization;
using System.Net;
using System.Reflection;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Email;

/// <summary>
/// Fills the embedded templates (<c>Templates/</c>). Every value is HTML-encoded for the HTML part: the customer's
/// name is typed by staff and must not be able to inject markup. The logo is attached inline (<c>cid:logo</c>) so it
/// shows even when the mail client blocks remote images.
/// </summary>
internal sealed class CustomerEmails(IOptions<PublicSiteOptions> site) : ICustomerEmails
{
    private const string SupportPhone = "0932 046 296";

    // Vietnam has no daylight saving time; a fixed offset avoids depending on tzdata in the container.
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private static readonly Lazy<string> InvitationHtml = new(() => ReadText("InvitationEmail.html"));
    private static readonly Lazy<string> InvitationText = new(() => ReadText("InvitationEmail.txt"));
    private static readonly Lazy<byte[]> Logo = new(() => ReadBytes("logo-email.png"));

    public EmailMessage Invitation(string toEmail, string fullName, string token, DateTime expiresAtUtc)
    {
        // The token rides in the fragment: browsers never send it to a server, so it stays out of access logs.
        var link = $"{site.Value.PublicBaseUrl.TrimEnd('/')}/activate#token={Uri.EscapeDataString(token)}";

        var expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc))
            .ToOffset(VietnamOffset)
            .ToString("HH:mm 'ngày' dd/MM/yyyy", CultureInfo.InvariantCulture);

        var values = new Dictionary<string, string>
        {
            ["fullName"] = fullName,
            ["link"] = link,
            ["expiresAt"] = expires,
            ["supportPhone"] = SupportPhone,
            ["supportPhoneTel"] = SupportPhone.Replace(" ", string.Empty, StringComparison.Ordinal),
        };

        return new EmailMessage(
            toEmail,
            fullName,
            "Bon Bocon — Mời bạn kích hoạt tài khoản học",
            Fill(InvitationHtml.Value, values, WebUtility.HtmlEncode),
            Fill(InvitationText.Value, values, value => value),
            [new EmailInlineImage("logo", "logo.png", "image/png", Logo.Value)]);
    }

    private static string Fill(string template, IReadOnlyDictionary<string, string> values, Func<string, string> encode)
    {
        var result = template;

        foreach (var (key, value) in values)
        {
            result = result.Replace("{{" + key + "}}", encode(value), StringComparison.Ordinal);
        }

        return result;
    }

    private static Stream Open(string name) =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream($"ChefLan.EmailTemplates.{name}")
            ?? throw new InvalidOperationException($"Email template '{name}' is not embedded in the assembly.");

    private static string ReadText(string name)
    {
        using var reader = new StreamReader(Open(name));
        return reader.ReadToEnd();
    }

    private static byte[] ReadBytes(string name)
    {
        using var stream = Open(name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
