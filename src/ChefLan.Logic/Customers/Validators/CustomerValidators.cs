using ChefLan.Commons.Geography;
using ChefLan.Logic.Customers.Contracts;
using FluentValidation;

namespace ChefLan.Logic.Customers.Validators;

internal sealed class CustomerRegisterRequestValidator : AbstractValidator<CustomerRegisterRequest>
{
    public CustomerRegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

internal sealed class CustomerLoginRequestValidator : AbstractValidator<CustomerLoginRequest>
{
    public CustomerLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

internal sealed class CustomerRefreshRequestValidator : AbstractValidator<CustomerRefreshRequest>
{
    public CustomerRefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
}

internal sealed class CustomerLogoutRequestValidator : AbstractValidator<CustomerLogoutRequest>
{
    public CustomerLogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
}

internal sealed class CustomerAdminRequestValidator : AbstractValidator<CustomerAdminRequest>
{
    public CustomerAdminRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.ProvinceCode)
            .Must(VietnamProvinces.IsValid)
            .WithMessage("'{PropertyName}' is not a known province code.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProvinceCode));
        RuleFor(x => x.FacebookProfileUrl)
            .MaximumLength(500)
            .Must(u => Uri.TryCreate(u!.Trim(), UriKind.Absolute, out var uri)
                       && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("'{PropertyName}' must be an http or https URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.FacebookProfileUrl));
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}

internal sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(p => ProfilePhone.TryNormalize(p, out _))
            .WithMessage("'{PropertyName}' must be 9 to 15 digits, with an optional leading +.");
        RuleFor(x => x.ProvinceCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(VietnamProvinces.IsValid)
            .WithMessage("'{PropertyName}' is not a known province code.");
    }
}

internal sealed class InvitationTokenRequestValidator : AbstractValidator<InvitationTokenRequest>
{
    public InvitationTokenRequestValidator() => RuleFor(x => x.Token).NotEmpty().MaximumLength(100);
}

internal sealed class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequest>
{
    public AcceptInvitationRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(100);
        // Same rule as registration.
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
