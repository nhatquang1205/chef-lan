using ChefLan.Commons.Results;

namespace ChefLan.Logic.Customers.Errors;

public static class CustomerErrors
{
    public static Error NotFound(long id) =>
        Error.NotFound("customer.not_found", $"Customer {id} was not found.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("customer.invalid_credentials", "Email or password is incorrect.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("customer.invalid_refresh_token", "The refresh token is invalid or expired.");

    public static readonly Error NotActivated =
        Error.Forbidden("customer.not_activated", "This customer has no password set yet. Send an invite first.");

    public static readonly Error AccountLocked =
        Error.Forbidden("customer.account_locked", "This account is temporarily locked. Try again later.");

    public static readonly Error AccountInactive =
        Error.Forbidden("customer.account_inactive", "This customer account is not active.");

    public static readonly Error NoEmail =
        Error.Validation("customer.no_email", "Add an email address to this customer before inviting them.");

    public static readonly Error AlreadyActive =
        Error.Conflict("customer.already_active", "This customer has already set a password.");

    /// <summary>One answer for unknown, used and expired tokens: nothing to learn from probing.</summary>
    public static readonly Error InviteInvalid =
        Error.Validation("customer.invite_invalid", "This invitation link is invalid or has expired.");

    public static readonly Error EmailSendFailed =
        Error.Upstream("email.send_failed", "The email could not be sent. Check the mail settings and try again.");

    public static Error EmailAlreadyUsed(string email) =>
        Error.Conflict("customer.email_already_used", $"A customer already uses {email}.");
}
