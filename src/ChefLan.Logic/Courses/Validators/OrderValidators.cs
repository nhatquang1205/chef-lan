using ChefLan.Commons.Money;
using ChefLan.Logic.Courses.Contracts.Requests;
using FluentValidation;

namespace ChefLan.Logic.Courses.Validators;

internal sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.CourseSlug) == (x.LessonId is not null))
            .OverridePropertyName("Item")
            .WithMessage("Give exactly one of courseSlug or lessonId.");
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.CourseSlug).MaximumLength(200);
        RuleFor(x => x.LessonId!.Value).GreaterThan(0).OverridePropertyName(nameof(CreateOrderRequest.LessonId)).When(x => x.LessonId is not null);
    }
}

internal sealed class StaffOrderQuoteRequestValidator : AbstractValidator<StaffOrderQuoteRequest>
{
    public StaffOrderQuoteRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.CourseIds).NotNull().NotEmpty().Must(ids => ids is null || ids.Count <= StaffOrderLimits.MaxItems)
            .WithMessage($"At most {StaffOrderLimits.MaxItems} courses per order.");
        RuleFor(x => x.CourseIds).Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Each course can be on the order once.");
        RuleForEach(x => x.CourseIds).GreaterThan(0);
    }
}

internal sealed class CreateStaffOrderRequestValidator : AbstractValidator<CreateStaffOrderRequest>
{
    public CreateStaffOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Items).NotNull().NotEmpty().Must(items => items is null || items.Count <= StaffOrderLimits.MaxItems)
            .WithMessage($"At most {StaffOrderLimits.MaxItems} courses per order.");
        RuleFor(x => x.Items).Must(items => items is null || items.Select(i => i.CourseId).Distinct().Count() == items.Count)
            .WithMessage("Each course can be on the order once.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.CourseId).GreaterThan(0);
            item.RuleFor(i => i.UnitPriceVnd!.Value).Must(Vnd.IsValid)
                .OverridePropertyName(nameof(StaffOrderItemRequest.UnitPriceVnd))
                .WithMessage("The price must be a whole amount of dong, 0 or more.")
                .When(i => i.UnitPriceVnd is not null);
        });
        RuleFor(x => x.Note).NotEmpty().MaximumLength(300);
        RuleFor(x => x.OrderDiscountVnd!.Value).Must(Vnd.IsValid)
            .OverridePropertyName(nameof(CreateStaffOrderRequest.OrderDiscountVnd))
            .WithMessage("The discount must be a whole amount of dong, 0 or more.")
            .When(x => x.OrderDiscountVnd is not null);
        RuleFor(x => x.OrderDiscountReason).NotEmpty().WithMessage("A discount needs a reason.")
            .When(x => x.OrderDiscountVnd is > 0);
        RuleFor(x => x.OrderDiscountReason).MaximumLength(300);
    }
}

internal static class StaffOrderLimits
{
    public const int MaxItems = 20;
}

internal sealed class MarkOrderPaidRequestValidator : AbstractValidator<MarkOrderPaidRequest>
{
    public MarkOrderPaidRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PaymentTransactionId!.Value).GreaterThan(0)
            .OverridePropertyName(nameof(MarkOrderPaidRequest.PaymentTransactionId))
            .When(x => x.PaymentTransactionId is not null);
    }
}

internal sealed class LinkPaymentRequestValidator : AbstractValidator<LinkPaymentRequest>
{
    public LinkPaymentRequestValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
    }
}

internal sealed class AdminOrderFilterValidator : AbstractValidator<AdminOrderFilter>
{
    public AdminOrderFilterValidator()
    {
        RuleFor(x => x.Q).MaximumLength(100);
    }
}
