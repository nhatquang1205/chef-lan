using ChefLan.Logic.Identity.Contracts;
using FluentValidation;

namespace ChefLan.Logic.Identity.Validators;

internal sealed class StaffLoginRequestValidator : AbstractValidator<StaffLoginRequest>
{
    public StaffLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

internal sealed class StaffRefreshRequestValidator : AbstractValidator<StaffRefreshRequest>
{
    public StaffRefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
}

internal sealed class StaffLogoutRequestValidator : AbstractValidator<StaffLogoutRequest>
{
    public StaffLogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
}

internal sealed class StaffChangePasswordRequestValidator : AbstractValidator<StaffChangePasswordRequest>
{
    public StaffChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage("'New Password' must differ from the current password.");
    }
}
