using ChefLan.Logic.Courses.Payments;

namespace ChefLan.Logic.Tests;

public class OrderQuoteTests
{
    private const long Levain = 100;

    private static QuoteCourse Course(long id, decimal price) => new(id, $"Course {id}", price);

    private static QuoteInclude Includes(long parent, decimal discount, long module = Levain) =>
        new(parent, module, "Nuôi men", discount, 0);

    private static readonly IReadOnlySet<long> Nothing = new HashSet<long>();

    private static QuotedCourse Of(IReadOnlyList<QuotedCourse> quote, long id) => quote.Single(q => q.CourseId == id);

    [Fact]
    public void Two_parents_sharing_a_module_charge_it_once_to_the_smaller_discount()
    {
        var quote = OrderQuote.Quote(
            [Course(1, 2_000_000), Course(2, 1_500_000)],
            [Includes(1, 300_000), Includes(2, 200_000)],
            Nothing);

        Assert.Equal(2_000_000 - 300_000, Of(quote, 1).SystemPriceVnd);
        Assert.Equal(1_500_000, Of(quote, 2).SystemPriceVnd);
        Assert.Empty(Of(quote, 2).Discounts);
        Assert.Equal("Nuôi men đã tính trong Course 2", Of(quote, 1).Discounts.Single().Reason);
    }

    [Fact]
    public void The_answer_does_not_depend_on_the_order_of_selection()
    {
        var includes = new[] { Includes(1, 300_000), Includes(2, 200_000) };

        var forward = OrderQuote.Quote([Course(1, 2_000_000), Course(2, 1_500_000)], includes, Nothing);
        var backward = OrderQuote.Quote([Course(2, 1_500_000), Course(1, 2_000_000)], includes, Nothing);

        Assert.Equal(Of(forward, 1).SystemPriceVnd, Of(backward, 1).SystemPriceVnd);
        Assert.Equal(Of(forward, 2).SystemPriceVnd, Of(backward, 2).SystemPriceVnd);
    }

    [Fact]
    public void Three_parents_pay_for_the_module_once()
    {
        var quote = OrderQuote.Quote(
            [Course(1, 2_000_000), Course(2, 2_000_000), Course(3, 2_000_000)],
            [Includes(1, 300_000), Includes(2, 250_000), Includes(3, 400_000)],
            Nothing);

        Assert.Equal(2_000_000 - 300_000, Of(quote, 1).SystemPriceVnd);
        Assert.Equal(2_000_000, Of(quote, 2).SystemPriceVnd);
        Assert.Equal(2_000_000 - 400_000, Of(quote, 3).SystemPriceVnd);
    }

    [Fact]
    public void When_the_module_is_selected_too_every_parent_gets_its_discount()
    {
        var quote = OrderQuote.Quote(
            [Course(1, 2_000_000), Course(2, 1_500_000), Course(Levain, 800_000)],
            [Includes(1, 300_000), Includes(2, 200_000)],
            Nothing);

        Assert.Equal(1_700_000, Of(quote, 1).SystemPriceVnd);
        Assert.Equal(1_300_000, Of(quote, 2).SystemPriceVnd);
        Assert.Equal(800_000, Of(quote, Levain).SystemPriceVnd);
        Assert.Equal("Đã có Nuôi men", Of(quote, 1).Discounts.Single().Reason);
    }

    [Fact]
    public void When_the_customer_already_has_the_module_every_parent_gets_its_discount()
    {
        var quote = OrderQuote.Quote(
            [Course(1, 2_000_000), Course(2, 1_500_000)],
            [Includes(1, 300_000), Includes(2, 200_000)],
            new HashSet<long> { Levain });

        Assert.Equal(1_700_000, Of(quote, 1).SystemPriceVnd);
        Assert.Equal(1_300_000, Of(quote, 2).SystemPriceVnd);
    }

    [Fact]
    public void A_discount_never_takes_a_course_below_zero()
    {
        var quote = OrderQuote.Quote(
            [Course(1, 100_000), Course(2, 1_000_000)],
            [Includes(1, 300_000), Includes(2, 200_000)],
            new HashSet<long> { Levain });

        Assert.Equal(0, Of(quote, 1).SystemPriceVnd);
        Assert.Equal(100_000, Of(quote, 1).Discounts.Single().AmountVnd);
    }

    [Fact]
    public void Equal_discounts_leave_the_cheaper_course_paying_in_full_then_the_lower_id()
    {
        var cheaper = OrderQuote.Quote(
            [Course(1, 2_000_000), Course(2, 1_500_000)],
            [Includes(1, 300_000), Includes(2, 300_000)],
            Nothing);

        Assert.Equal(2_000_000 - 300_000, Of(cheaper, 1).SystemPriceVnd);
        Assert.Equal(1_500_000, Of(cheaper, 2).SystemPriceVnd);

        var sameAll = OrderQuote.Quote(
            [Course(7, 1_000_000), Course(3, 1_000_000)],
            [Includes(7, 300_000), Includes(3, 300_000)],
            Nothing);

        Assert.Equal(1_000_000, Of(sameAll, 3).SystemPriceVnd);
        Assert.Equal(700_000, Of(sameAll, 7).SystemPriceVnd);
    }

    [Fact]
    public void A_single_parent_pays_in_full_when_nothing_else_has_the_module()
    {
        var quote = OrderQuote.Quote([Course(1, 2_000_000)], [Includes(1, 300_000)], Nothing);

        Assert.Equal(2_000_000, Of(quote, 1).SystemPriceVnd);
        Assert.Empty(Of(quote, 1).Discounts);
    }

    [Fact]
    public void The_order_discount_is_split_in_proportion_and_the_remainder_goes_to_the_most_expensive()
    {
        var quoted = Quoted((1, 1_000_000), (2, 2_000_000));

        var priced = OrderQuote.Price(quoted, new Dictionary<long, decimal>(), 100_000, "Khách quen");

        Assert.Equal(33_333, priced.Single(p => p.CourseId == 1).DiscountVnd);
        Assert.Equal(66_667, priced.Single(p => p.CourseId == 2).DiscountVnd);
        Assert.Equal(3_000_000 - 100_000, priced.Sum(p => p.UnitPriceVnd));
        Assert.All(priced, p => Assert.Equal("Khách quen", p.DiscountReason));
    }

    [Fact]
    public void Staff_edits_come_before_the_order_discount_and_are_named_in_the_reason()
    {
        var quoted = new[]
        {
            new QuotedCourse(1, "A", 1_000_000, [new QuoteLineDiscount(100, "Nuôi men", 200_000, "Đã có Nuôi men")], 800_000),
            new QuotedCourse(2, "B", 2_000_000, [], 2_000_000),
        };

        var priced = OrderQuote.Price(quoted, new Dictionary<long, decimal> { [1] = 600_000, [2] = 2_000_000 }, 260_000, "Giảm thêm");

        var a = priced.Single(p => p.CourseId == 1);
        var b = priced.Single(p => p.CourseId == 2);

        Assert.Equal(540_000, a.UnitPriceVnd);
        Assert.Equal(1_000_000 - 540_000, a.DiscountVnd);
        Assert.Equal("Đã có Nuôi men; Giá chỉnh tay; Giảm thêm", a.DiscountReason);
        Assert.Equal(1_800_000, b.UnitPriceVnd);
        Assert.Equal("Giảm thêm", b.DiscountReason);
        Assert.Equal(2_340_000, priced.Sum(p => p.UnitPriceVnd));
    }

    [Fact]
    public void A_price_left_at_the_system_price_is_not_called_manual()
    {
        var priced = OrderQuote.Price(Quoted((1, 1_000_000)), new Dictionary<long, decimal> { [1] = 1_000_000 }, 0, null);

        Assert.Null(priced.Single().DiscountReason);
        Assert.Equal(0, priced.Single().DiscountVnd);
    }

    [Fact]
    public void The_remainder_never_pushes_an_item_below_zero()
    {
        var priced = OrderQuote.Price(Quoted((1, 1), (2, 1), (3, 1)), new Dictionary<long, decimal>(), 2, "x");

        Assert.All(priced, p => Assert.InRange(p.UnitPriceVnd, 0, 1));
        Assert.Equal(1, priced.Sum(p => p.UnitPriceVnd));
    }

    private static List<QuotedCourse> Quoted(params (long Id, decimal Price)[] courses) =>
        courses.Select(c => new QuotedCourse(c.Id, $"Course {c.Id}", c.Price, [], c.Price)).ToList();
}
