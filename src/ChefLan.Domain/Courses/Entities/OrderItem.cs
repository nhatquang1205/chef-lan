using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

/// <summary>One thing on an order. Titles and prices are snapshots, so later edits never rewrite history.</summary>
public class OrderItem
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public OrderItemKind Kind { get; set; }

    public long? CourseId { get; set; }

    public long? LessonId { get; set; }

    public required string Title { get; set; }

    /// <summary>The course's slug, so the site can link to what was bought. For a lesson: its course's slug.</summary>
    public required string CourseSlug { get; set; }

    public decimal ListPriceVnd { get; set; }

    public decimal DiscountVnd { get; set; }

    public string? DiscountReason { get; set; }

    /// <summary>What this item costs: list price minus discount.</summary>
    public decimal UnitPriceVnd { get; set; }

    public Order? Order { get; set; }
}
