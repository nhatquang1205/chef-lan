using ChefLan.Infrastructure.Services.Notifications;
using ChefLan.Logic.Courses.Contracts;

namespace ChefLan.Logic.Tests;

public class ContactOrderMessageTests
{
    private static ContactOrderNotice Notice(
        string name = "Nguyễn Văn A",
        string? phone = "0901234567",
        string? province = "Hà Nội",
        string course = "Macaron",
        string? lesson = null) =>
        new("CL-ABC234", name, "a@example.com", phone, province, course, lesson, 550_000m, new DateTime(2026, 11, 19, 3, 30, 0, DateTimeKind.Utc));

    [Fact]
    public void The_message_has_every_line_in_vietnam_time()
    {
        var text = ContactOrderMessage.Build(Notice(), "https://admin.example.com/");

        Assert.Equal(
            "🛒 Đơn liên hệ mới **CL-ABC234**\n"
            + "Khách: Nguyễn Văn A · a@example.com · 0901234567 · Hà Nội\n"
            + "Khóa học: Macaron\n"
            + "Số tiền: 550.000 đ\n"
            + "Giữ tới: 19/11/2026 10:30\n"
            + "https://admin.example.com/orders?q=CL-ABC234",
            text.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void A_lesson_names_its_course_and_an_empty_admin_url_drops_the_link()
    {
        var text = ContactOrderMessage.Build(Notice(lesson: "Vỏ bánh"), "");

        Assert.Contains("Bài học: Vỏ bánh — Macaron", text);
        Assert.DoesNotContain("orders?q=", text);
    }

    [Fact]
    public void Markdown_in_customer_text_is_escaped()
    {
        var text = ContactOrderMessage.Build(Notice(name: "*bold* _i_ ~s~ `c` a|b > q"), "");

        Assert.Contains(@"\*bold\* \_i\_ \~s\~ \`c\` a\|b \> q", text);
    }

    [Fact]
    public void Missing_phone_and_province_show_a_dash()
    {
        var text = ContactOrderMessage.Build(Notice(phone: null, province: " "), "");

        Assert.Contains("a@example.com · — · —", text);
    }

    [Fact]
    public void The_message_stays_under_the_discord_limit()
    {
        var text = ContactOrderMessage.Build(Notice(name: new string('x', 5000), course: new string('y', 5000), lesson: new string('z', 5000)), "https://admin.example.com");

        Assert.True(text.Length <= ContactOrderMessage.MaxLength);
    }
}
