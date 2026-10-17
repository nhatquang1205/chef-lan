using ChefLan.Commons.Money;
using ChefLan.Logic.Courses.Contracts.Requests;
using FluentValidation;

namespace ChefLan.Logic.Courses.Validators;

internal sealed class CourseIncludeRequestValidator : AbstractValidator<CourseIncludeRequest>
{
    public CourseIncludeRequestValidator() =>
        RuleFor(x => x.DiscountVnd)
            .Must(Vnd.IsValid).WithMessage("'Discount Vnd' must be a whole, non-negative number of dong.")
            .LessThan(1_000_000_000_000m);
}

internal sealed class CourseIncludeOrderRequestValidator : AbstractValidator<CourseIncludeOrderRequest>
{
    public CourseIncludeOrderRequestValidator() => RuleFor(x => x.IncludedCourseIds).NotNull();
}
