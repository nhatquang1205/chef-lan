namespace ChefLan.Logic.Courses.Contracts;

/// <summary>What staff need to see when a customer asks to be contacted about an order. Already resolved: names, not ids.</summary>
public sealed record ContactOrderNotice(
    string OrderCode,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? ProvinceName,
    string? CourseTitle,
    string? LessonTitle,
    decimal AmountVnd,
    DateTime HeldUntilUtc);
