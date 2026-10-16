# B1 — Real email sending through Gmail

**Goal:** `IEmailSender` actually sends mail through Gmail SMTP, with an inline image, in any environment where
SMTP is configured. No feature uses it yet beyond a manual check.

## Build

1. **Package:** `MailKit` (Central Package Management: version in `Directory.Packages.props`, no version in the csproj),
   referenced by `ChefLan.Infrastructure` only. `System.Net.Mail.SmtpClient` is not used (obsolete for new code).
2. **Port** (`ChefLan.Logic/Abstractions/IEmailSender.cs`): extend to a message object so inline images and a text
   part fit:
   ```csharp
   public sealed record EmailMessage(string ToEmail, string? ToName, string Subject, string HtmlBody, string TextBody,
       IReadOnlyList<EmailInlineImage> InlineImages);
   public sealed record EmailInlineImage(string ContentId, string FileName, string ContentType, byte[] Content);
   Task SendAsync(EmailMessage message, CancellationToken ct = default);
   ```
   Update `LoggingEmailSender` (logs to, subject and the text body; never the HTML with images).
3. **`SmtpEmailSender`** (`ChefLan.Infrastructure/Services/Email/`): MailKit `SmtpClient`, `SecureSocketOptions.StartTls`,
   authenticate with `Smtp:Username` / `Smtp:Password` (the Gmail **app password**), From = `Smtp:FromAddress` /
   `Smtp:FromDisplayName` ("Bon Bocon"). `multipart/alternative` (text + html) with the inline images as
   `multipart/related` linked resources (`Content-Id`). Timeout ~30 s. Throw on failure; callers decide.
4. **Choosing the sender** (`InfrastructureServiceCollectionExtensions`): use `SmtpEmailSender` whenever `Smtp:Host`
   is set, in **any** environment; otherwise `LoggingEmailSender` in Development, and in Production log a startup
   **warning** "SMTP not configured, emails are only logged" (do not crash: the site runs without invites).
5. **Config** (`appsettings.json` keeps empty values; secrets via user-secrets locally, `.env` on the VPS):
   ```
   Smtp__Host=smtp.gmail.com
   Smtp__Port=587
   Smtp__Username=<gmail address>
   Smtp__Password=<16-char app password, no spaces>
   Smtp__FromAddress=<same gmail address>   # Gmail rewrites any other From to the account anyway
   Smtp__FromDisplayName=Bon Bocon
   ```
   Document in `deploy/README.md` and `deploy/.env.example` (if present) + `CLAUDE.md` (replace the "SmtpEmailSender
   unimplemented" warning). How to get an app password: Google Account → Security → 2-Step Verification must be on →
   App passwords → create "Bon Bocon API".

## Out of scope

Templates and any feature mail (B3). Queues/retries: an invite is sent inline in the request; failure is reported
to the admin, who can press resend.

## Done when

- `dotnet build ChefLan.slnx -c Release -warnaserror` clean.
- With the Gmail values in user-secrets, a temporary dev-only check (a test, or a one-off `dotnet run` snippet that
  is not committed) delivers a mail with an inline image to the owner's inbox; it is not in spam.
- Without `Smtp:Host`, Development logs the mail instead of sending.
