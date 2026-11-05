using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class OrderErrors
{
    public static readonly Error CheckoutDisabled =
        Error.Unavailable("order.checkout_disabled", "Online payment is not available right now. Please contact chef Lan.");

    public static readonly Error ItemRequired =
        Error.Validation("order.item_required", "Give exactly one of courseSlug or lessonId.");

    public static readonly Error NotForSale =
        Error.Validation("order.not_for_sale", "This cannot be bought online.");

    public static readonly Error AlreadyOwned =
        Error.Conflict("order.already_owned", "You already have access to this.");

    public static readonly Error AlreadyPaid =
        Error.Conflict("order.already_paid", "This order is already paid.");

    public static readonly Error NotPending =
        Error.Conflict("order.not_pending", "Only a pending order can be cancelled.");

    public static readonly Error TransactionLinked =
        Error.Conflict("order.transaction_linked", "That bank transaction already belongs to another order.");

    public static readonly Error TransactionSettled =
        Error.Conflict("order.transaction_settled", "That bank transaction has already been applied to an order.");

    public static Error NotFound(string code) => Error.NotFound("order.not_found", $"Order {code} was not found.");

    public static Error NotFound(long id) => Error.NotFound("order.not_found", $"Order {id} was not found.");

    public static Error TransactionNotFound(long id) =>
        Error.NotFound("order.transaction_not_found", $"Bank transaction {id} was not found.");
}
