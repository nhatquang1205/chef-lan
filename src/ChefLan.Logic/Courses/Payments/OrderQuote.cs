namespace ChefLan.Logic.Courses.Payments;

/// <summary>A course on the order, at its list price.</summary>
public sealed record QuoteCourse(long CourseId, string Title, decimal ListPriceVnd);

/// <summary>"Parent includes module; a customer who has the module gets this much off the parent."</summary>
public sealed record QuoteInclude(long CourseId, long IncludedCourseId, string IncludedTitle, decimal DiscountVnd, int SortOrder);

public sealed record QuoteLineDiscount(long IncludedCourseId, string Title, decimal AmountVnd, string Reason);

public sealed record QuotedCourse(
    long CourseId,
    string Title,
    decimal ListPriceVnd,
    IReadOnlyList<QuoteLineDiscount> Discounts,
    decimal SystemPriceVnd);

/// <summary>What one course really costs once staff's edits and the order discount are in.</summary>
public sealed record PricedCourse(
    long CourseId,
    decimal ListPriceVnd,
    decimal UnitPriceVnd,
    decimal DiscountVnd,
    string? DiscountReason);

/// <summary>
/// The pricing rules of an order staff create, with no data access. First the shared-module rule, then the split
/// of staff's edits and whole-order discount over the items.
/// </summary>
public static class OrderQuote
{
    public const int MaxReasonLength = 300;

    public const string ManualPriceReason = "Giá chỉnh tay";

    /// <summary>
    /// The shared-module rule. For each module C included by a selected course: if C is selected too, or the customer already
    /// has it, every selected course that includes it gets its discount for C; otherwise C is charged once, to the course with
    /// the smallest discount for it (ties: cheaper list price, then lower id), and the others get theirs. The answer does not
    /// depend on the order of <paramref name="courses"/>. A course never goes below 0.
    /// </summary>
    public static IReadOnlyList<QuotedCourse> Quote(
        IReadOnlyList<QuoteCourse> courses,
        IReadOnlyList<QuoteInclude> includes,
        IReadOnlySet<long> customerHas)
    {
        var selected = courses.Select(c => c.CourseId).ToHashSet();
        var byId = courses.ToDictionary(c => c.CourseId);
        var rows = includes.Where(i => selected.Contains(i.CourseId)).ToList();

        var payer = new Dictionary<long, long>();

        foreach (var module in rows.GroupBy(i => i.IncludedCourseId))
        {
            if (selected.Contains(module.Key) || customerHas.Contains(module.Key))
            {
                continue;
            }

            payer[module.Key] = module
                .OrderBy(i => i.DiscountVnd)
                .ThenBy(i => byId[i.CourseId].ListPriceVnd)
                .ThenBy(i => i.CourseId)
                .First().CourseId;
        }

        return courses.Select(course =>
        {
            var remaining = course.ListPriceVnd;
            var discounts = new List<QuoteLineDiscount>();

            foreach (var row in rows
                .Where(i => i.CourseId == course.CourseId && i.DiscountVnd > 0)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.IncludedCourseId))
            {
                var paysFull = payer.TryGetValue(row.IncludedCourseId, out var payerId) && payerId == course.CourseId;

                if (paysFull)
                {
                    continue;
                }

                var amount = Math.Min(row.DiscountVnd, remaining);

                if (amount <= 0)
                {
                    break;
                }

                remaining -= amount;

                var reason = payer.TryGetValue(row.IncludedCourseId, out var by)
                    ? $"{row.IncludedTitle} đã tính trong {byId[by].Title}"
                    : $"Đã có {row.IncludedTitle}";

                discounts.Add(new QuoteLineDiscount(row.IncludedCourseId, row.IncludedTitle, amount, reason));
            }

            return new QuotedCourse(course.CourseId, course.Title, course.ListPriceVnd, discounts, remaining);
        }).ToList();
    }

    /// <summary>
    /// Applies staff's per-course prices (absent = the system price) and then shares <paramref name="orderDiscountVnd"/> over the
    /// items in proportion to their price, in whole dong; what is left over goes to the most expensive item. The unit prices add
    /// up to the order total. Inputs are assumed valid: prices are whole dong within 0..list, the discount at most their sum.
    /// </summary>
    public static IReadOnlyList<PricedCourse> Price(
        IReadOnlyList<QuotedCourse> quoted,
        IReadOnlyDictionary<long, decimal> editedPrices,
        decimal orderDiscountVnd,
        string? orderDiscountReason)
    {
        var edited = quoted.Select(q => (
            Quote: q,
            Price: editedPrices.TryGetValue(q.CourseId, out var p) ? p : q.SystemPriceVnd,
            WasEdited: editedPrices.TryGetValue(q.CourseId, out var e) && e != q.SystemPriceVnd)).ToList();

        var sum = edited.Sum(x => x.Price);
        var shares = edited.ToDictionary(x => x.Quote.CourseId, _ => 0m);

        if (orderDiscountVnd > 0 && sum > 0)
        {
            foreach (var x in edited)
            {
                shares[x.Quote.CourseId] = decimal.Floor(orderDiscountVnd * x.Price / sum);
            }

            var left = orderDiscountVnd - shares.Values.Sum();

            foreach (var x in edited.OrderByDescending(x => x.Price).ThenBy(x => x.Quote.CourseId))
            {
                if (left <= 0)
                {
                    break;
                }

                var room = x.Price - shares[x.Quote.CourseId];
                var extra = Math.Min(room, left);
                shares[x.Quote.CourseId] += extra;
                left -= extra;
            }
        }

        return edited.Select(x =>
        {
            var share = shares[x.Quote.CourseId];
            var unit = x.Price - share;
            var reasons = x.Quote.Discounts.Select(d => d.Reason).ToList();

            if (x.WasEdited)
            {
                reasons.Add(ManualPriceReason);
            }

            if (share > 0 && !string.IsNullOrWhiteSpace(orderDiscountReason))
            {
                reasons.Add(orderDiscountReason.Trim());
            }

            return new PricedCourse(
                x.Quote.CourseId,
                x.Quote.ListPriceVnd,
                unit,
                x.Quote.ListPriceVnd - unit,
                reasons.Count == 0 ? null : Truncate(string.Join("; ", reasons)));
        }).ToList();
    }

    private static string Truncate(string text) => text.Length <= MaxReasonLength ? text : text[..(MaxReasonLength - 1)] + "…";
}
