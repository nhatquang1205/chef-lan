using System.Text;
using ChefLan.Commons.Money;
using ChefLan.Logic.Courses.Contracts;

namespace ChefLan.Infrastructure.Services.Notifications;

internal static class ContactOrderMessage
{
    public const int MaxLength = 2000;
    private const int MaxFieldLength = 120;
    private const string Missing = "—";
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static string Build(ContactOrderNotice notice, string adminBaseUrl)
    {
        var item = notice.LessonTitle is null
            ? $"Khóa học: {Clean(notice.CourseTitle)}"
            : $"Bài học: {Clean(notice.LessonTitle)} — {Clean(notice.CourseTitle)}";

        var heldUntil = (notice.HeldUntilUtc + VietnamOffset).ToString("dd/MM/yyyy HH:mm");

        var text = new StringBuilder()
            .AppendLine($"🛒 Đơn liên hệ mới **{notice.OrderCode}**")
            .AppendLine($"Khách: {Clean(notice.CustomerName)} · {Clean(notice.CustomerEmail)} · {Clean(notice.CustomerPhone)} · {Clean(notice.ProvinceName)}")
            .AppendLine(item)
            .AppendLine($"Số tiền: {Vnd.ToDisplay(notice.AmountVnd).Replace('₫', 'đ')}")
            .Append($"Giữ tới: {heldUntil}");

        if (!string.IsNullOrWhiteSpace(adminBaseUrl))
        {
            text.Append($"\n{adminBaseUrl.TrimEnd('/')}/orders?q={Uri.EscapeDataString(notice.OrderCode)}");
        }

        var result = text.ToString();

        return result.Length <= MaxLength ? result : result[..MaxLength];
    }

    /// <summary>Customer-typed text: markdown neutralised, one line, cut short, "—" when empty.</summary>
    internal static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Missing;
        }

        var flat = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (flat.Length > MaxFieldLength)
        {
            flat = flat[..MaxFieldLength] + "…";
        }

        var escaped = new StringBuilder(flat.Length + 8);

        foreach (var c in flat)
        {
            if (c is '*' or '_' or '~' or '`' or '|' or '>' or '\\' or '[' or ']')
            {
                escaped.Append('\\');
            }

            escaped.Append(c);
        }

        return escaped.ToString();
    }
}
