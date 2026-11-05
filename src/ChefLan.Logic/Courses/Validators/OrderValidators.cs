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
        RuleFor(x => x.CourseSlug).MaximumLength(200);
        RuleFor(x => x.LessonId!.Value).GreaterThan(0).OverridePropertyName(nameof(CreateOrderRequest.LessonId)).When(x => x.LessonId is not null);
    }
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
