using ChefLan.Logic.Courses.Contracts.Requests;
using FluentValidation;

namespace ChefLan.Logic.Courses.Validators;

internal sealed class CourseImageRequestValidator : AbstractValidator<CourseImageRequest>
{
    public CourseImageRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(1000).MustBeHttpUrl();
        RuleFor(x => x.Alt).MaximumLength(200);
        RuleFor(x => x.Width).GreaterThan(0).When(x => x.Width is not null);
        RuleFor(x => x.Height).GreaterThan(0).When(x => x.Height is not null);
    }
}

internal sealed class CourseImageUpdateRequestValidator : AbstractValidator<CourseImageUpdateRequest>
{
    public CourseImageUpdateRequestValidator() => RuleFor(x => x.Alt).MaximumLength(200);
}

internal sealed class CourseImageOrderRequestValidator : AbstractValidator<CourseImageOrderRequest>
{
    public CourseImageOrderRequestValidator() => RuleFor(x => x.ImageIds).NotNull();
}
