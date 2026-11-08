using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Payments;

namespace ChefLan.Logic.Tests;

public class IncludeDiscountsTests
{
    private static CourseInclude Include(long module, decimal discount) =>
        new() { CourseId = 1, IncludedCourseId = module, DiscountVnd = discount, IncludedCourse = new Course { Title = $"Module {module}", Slug = $"module-{module}" } };

    private static IReadOnlySet<long> Owns(params long[] ids) => ids.ToHashSet();

    [Fact]
    public void No_owned_module_leaves_the_list_price()
    {
        var (discounts, final) = IncludeDiscounts.Apply(1_500_000, [Include(10, 300_000)], Owns());

        Assert.Empty(discounts);
        Assert.Equal(1_500_000, final);
    }

    [Fact]
    public void One_owned_module_takes_its_discount_off()
    {
        var (discounts, final) = IncludeDiscounts.Apply(1_500_000, [Include(10, 300_000), Include(11, 100_000)], Owns(10));

        Assert.Equal(1_200_000, final);
        var discount = Assert.Single(discounts);
        Assert.Equal(10, discount.IncludedCourseId);
        Assert.Equal(300_000, discount.DiscountVnd);
    }

    [Fact]
    public void Two_modules_are_capped_at_the_list_price()
    {
        var (discounts, final) = IncludeDiscounts.Apply(500_000, [Include(10, 400_000), Include(11, 400_000)], Owns(10, 11));

        Assert.Equal(0, final);
        Assert.Equal([400_000m, 100_000m], discounts.Select(d => d.DiscountVnd));
    }

    [Fact]
    public void A_zero_discount_is_ignored()
    {
        var (discounts, final) = IncludeDiscounts.Apply(500_000, [Include(10, 0)], Owns(10));

        Assert.Empty(discounts);
        Assert.Equal(500_000, final);
    }
}
