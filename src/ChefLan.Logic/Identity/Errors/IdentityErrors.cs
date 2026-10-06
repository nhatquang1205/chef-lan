using ChefLan.Commons.Results;

namespace ChefLan.Logic.Identity.Errors;

public static class IdentityErrors
{
    public static Error UserNotFound(long id) =>
        Error.NotFound("identity.user_not_found", $"Staff user {id} was not found.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("identity.invalid_credentials", "Email or password is incorrect.");

    public static readonly Error AccountLocked =
        Error.Forbidden("identity.account_locked", "This account is temporarily locked. Try again later.");

    public static readonly Error AccountInactive =
        Error.Forbidden("identity.account_inactive", "This staff account is no longer active.");

    public static Error EmailAlreadyUsed(string email) =>
        Error.Conflict("identity.email_already_used", $"A staff account already uses {email}.");
}
