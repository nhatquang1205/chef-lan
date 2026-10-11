using ChefLan.Commons.Money;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using FluentValidation;

namespace ChefLan.Logic.Courses.Validators;

internal static class ValidatorExtensions
{
    /// <summary>
    /// http(s) only. These URLs are rendered by a frontend, so a stored javascript: or
    /// data: URI would be a stored-XSS vector.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeHttpUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(value => string.IsNullOrWhiteSpace(value)
                || (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("'{PropertyName}' must be an absolute http or https URL.");
}

internal sealed class CategoryRequestValidator : AbstractValidator<CategoryRequest>
{
    public CategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(1000).MustBeHttpUrl();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder is not null);
        RuleFor(x => x.Status).IsInEnum();
    }
}

internal sealed class CourseRequestValidator : AbstractValidator<CourseRequest>
{
    public CourseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subtitle).MaximumLength(300);
        RuleFor(x => x.MetaDescription).MaximumLength(300);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(1000).MustBeHttpUrl();
        RuleFor(x => x.PromoVideoUrl).MaximumLength(1000).MustBeHttpUrl();
        RuleFor(x => x.LegacyFacebookGroupUrl).MaximumLength(1000).MustBeHttpUrl();
        RuleFor(x => x.Level).IsInEnum();

        // Postgres numeric(12,0) ROUNDS a fraction instead of rejecting it, so this is the
        // only place 1200000.50 is caught before it silently becomes 1200001.
        RuleFor(x => x.PriceVnd)
            .Must(Vnd.IsValid).WithMessage("'Price Vnd' must be a whole, non-negative number of dong.")
            .LessThan(1_000_000_000_000m);

        When(x => x.CompareAtPriceVnd is not null, () =>
            RuleFor(x => x.CompareAtPriceVnd!.Value)
                .Must(Vnd.IsValid).WithMessage("'Compare At Price Vnd' must be a whole, non-negative number of dong.")
                .GreaterThanOrEqualTo(x => x.PriceVnd)
                .WithMessage("'Compare At Price Vnd' must not be below the price.")
                .OverridePropertyName(nameof(CourseRequest.CompareAtPriceVnd)));

        RuleFor(x => x.AccessDurationDays).GreaterThan(0).When(x => x.AccessDurationDays is not null);
        RuleForEach(x => x.CategoryIds).GreaterThan(0);
    }
}

internal sealed class LessonRequestValidator : AbstractValidator<LessonRequest>
{
    public LessonRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Recipes).MaximumLength(100_000);
        RuleFor(x => x.ResourcesUrl).MaximumLength(1000).MustBeHttpUrl();
    }
}

internal sealed class LessonOrderRequestValidator : AbstractValidator<LessonOrderRequest>
{
    public LessonOrderRequestValidator() => RuleFor(x => x.LessonIds).NotNull();
}

internal sealed class LessonVideoOrderRequestValidator : AbstractValidator<LessonVideoOrderRequest>
{
    public LessonVideoOrderRequestValidator() => RuleFor(x => x.VideoIds).NotNull();
}

internal sealed class LessonVideoRequestValidator : AbstractValidator<LessonVideoRequest>
{
    public LessonVideoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.ExternalId).MaximumLength(100);
        RuleFor(x => x.Url).MaximumLength(1000).MustBeHttpUrl();
        RuleFor(x => x.ThumbnailUrl).MaximumLength(1000).MustBeHttpUrl();
        RuleFor(x => x.DurationSeconds).GreaterThanOrEqualTo(0);

        // The DB CHECKs would reject these too; the 400 is so the admin UI gets a usable
        // message instead of a 500.
        When(x => x.FreeTillSeconds is not null, () =>
        {
            RuleFor(x => x.FreeTillSeconds!.Value)
                .GreaterThan(0)
                .OverridePropertyName(nameof(LessonVideoRequest.FreeTillSeconds));
            RuleFor(x => x.IsFree)
                .Equal(true).WithMessage("'Free Till Seconds' can only be set on a free video.");
        });

        // A draft video may have no source yet; a published one must be playable.
        When(x => x.IsPublished, () =>
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.ExternalId) || !string.IsNullOrWhiteSpace(x.Url))
                .OverridePropertyName("Source")
                .WithMessage("A published video needs an externalId or a url.");

            RuleFor(x => x.ExternalId)
                .NotEmpty().When(x => x.Provider == VideoProvider.Bunny)
                .WithMessage("A published Bunny video needs its externalId (the Bunny video GUID).");
        });
    }
}
