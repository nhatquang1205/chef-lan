using ChefLan.Commons.Money;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using FluentValidation;

namespace ChefLan.Logic.Courses.Validators;

internal static class AccessRules
{
    public static bool ExactlyOneCustomer(long? id, string? email) => (id is not null) ^ !string.IsNullOrWhiteSpace(email);
}

internal sealed class GrantCourseAccessRequestValidator : AbstractValidator<GrantCourseAccessRequest>
{
    public GrantCourseAccessRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId is not null);
        RuleFor(x => x.CustomerEmail).MaximumLength(254);
        RuleFor(x => x)
            .Must(x => AccessRules.ExactlyOneCustomer(x.CustomerId, x.CustomerEmail))
            .OverridePropertyName("Customer")
            .WithMessage("Give exactly one of customerId or customerEmail.");
        RuleFor(x => x.AmountPaidVnd!.Value)
            .Must(Vnd.IsValid).WithMessage("'Amount Paid Vnd' must be a whole, non-negative number of dong.")
            .OverridePropertyName(nameof(GrantCourseAccessRequest.AmountPaidVnd))
            .When(x => x.AmountPaidVnd is not null);
        RuleFor(x => x.IsPaid).Equal(true).When(x => x.AmountPaidVnd is not null)
            .WithMessage("An amount can only be recorded when isPaid is true.");
        RuleFor(x => x.PaymentNote).MaximumLength(500);
        RuleFor(x => x.PaymentReference).MaximumLength(100);
        RuleFor(x => x.Source)
            .Must(s => s is EnrollmentSource.AdminGrant or EnrollmentSource.FacebookMigration)
            .WithMessage("Source must be AdminGrant or FacebookMigration.");
    }
}

internal sealed class GrantLessonAccessRequestValidator : AbstractValidator<GrantLessonAccessRequest>
{
    public GrantLessonAccessRequestValidator()
    {
        RuleFor(x => x.LessonId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId is not null);
        RuleFor(x => x.CustomerEmail).MaximumLength(254);
        RuleFor(x => x)
            .Must(x => AccessRules.ExactlyOneCustomer(x.CustomerId, x.CustomerEmail))
            .OverridePropertyName("Customer")
            .WithMessage("Give exactly one of customerId or customerEmail.");
        RuleFor(x => x.AmountPaidVnd!.Value)
            .Must(Vnd.IsValid).WithMessage("'Amount Paid Vnd' must be a whole, non-negative number of dong.")
            .OverridePropertyName(nameof(GrantLessonAccessRequest.AmountPaidVnd))
            .When(x => x.AmountPaidVnd is not null);
        RuleFor(x => x.IsPaid).Equal(true).When(x => x.AmountPaidVnd is not null)
            .WithMessage("An amount can only be recorded when isPaid is true.");
        RuleFor(x => x.PaymentNote).MaximumLength(500);
        RuleFor(x => x.PaymentReference).MaximumLength(100);
        RuleFor(x => x.Source)
            .Must(s => s is EnrollmentSource.AdminGrant or EnrollmentSource.FacebookMigration)
            .WithMessage("Source must be AdminGrant or FacebookMigration.");
    }
}
