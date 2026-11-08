using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Payments;

/// <summary>
/// The discount a course gets for shared modules the customer already owns. One rule for the course quote and the
/// catalog state, so the two can never disagree.
/// </summary>
internal static class IncludeDiscounts
{
    /// <summary>
    /// Each owned module takes its discount off, in the order of <paramref name="includes"/>, capped so the running
    /// total never passes the list price (the course may have been cheapened after the discount was set).
    /// </summary>
    public static (IReadOnlyList<QuoteDiscountResponse> Discounts, decimal FinalPriceVnd) Apply(
        decimal listPriceVnd,
        IEnumerable<CourseInclude> includes,
        IReadOnlySet<long> ownedCourseIds)
    {
        var remaining = listPriceVnd;
        var discounts = new List<QuoteDiscountResponse>();

        foreach (var row in includes.Where(r => ownedCourseIds.Contains(r.IncludedCourseId) && r.DiscountVnd > 0))
        {
            var amount = Math.Min(row.DiscountVnd, remaining);

            if (amount <= 0)
            {
                break;
            }

            remaining -= amount;
            discounts.Add(new QuoteDiscountResponse(
                row.IncludedCourseId,
                row.IncludedCourse?.Title ?? string.Empty,
                amount,
                $"Đã có khóa {row.IncludedCourse?.Title}"));
        }

        return (discounts, remaining);
    }
}
