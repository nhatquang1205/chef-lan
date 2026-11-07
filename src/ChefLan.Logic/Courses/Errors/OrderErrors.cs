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

    public static readonly Error TotalNotPositive =
        Error.Validation("order.total_not_positive", "The order total must be more than 0. Use a manual grant for a free gift.");

    public static readonly Error DiscountTooLarge =
        Error.Validation("order.discount_too_large", "The order discount cannot be more than the items add up to.");

    public static Error AlreadyOwnedCourse(string title) =>
        Error.Conflict("order.already_owned", $"The customer already has access to \"{title}\".");

    public static Error PriceOutOfRange(string title) =>
        Error.Validation("order.price_out_of_range", $"The price of \"{title}\" must be a whole amount between 0 and its list price.");

    public static Error NotFound(string code) => Error.NotFound("order.not_found", $"Order {code} was not found.");

    public static Error NotFound(long id) => Error.NotFound("order.not_found", $"Order {id} was not found.");

    public static Error TransactionNotFound(long id) =>
        Error.NotFound("order.transaction_not_found", $"Bank transaction {id} was not found.");
}
