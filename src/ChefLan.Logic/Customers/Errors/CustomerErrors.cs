using ChefLan.Commons.Results;

namespace ChefLan.Logic.Customers.Errors;

public static class CustomerErrors
{
    public static Error NotFound(long id) =>
        Error.NotFound("customer.not_found", $"Customer {id} was not found.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("customer.invalid_credentials", "Email or password is incorrect.");

    public static readonly Error NotActivated =
        Error.Forbidden("customer.not_activated", "This customer has no password set yet. Send an invite first.");

    public static readonly Error AccountLocked =
        Error.Forbidden("customer.account_locked", "This account is temporarily locked. Try again later.");

    public static readonly Error AccountInactive =
        Error.Forbidden("customer.account_inactive", "This customer account is not active.");

    public static Error EmailAlreadyUsed(string email) =>
        Error.Conflict("customer.email_already_used", $"A customer already uses {email}.");
}
