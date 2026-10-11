using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class EnrollmentErrors
{
    public static readonly Error CustomerRequired =
        Error.Validation("enrollment.customer_required", "Give exactly one of customerId or customerEmail.");

    public static readonly Error CustomerNotFound =
        Error.NotFound("enrollment.customer_not_found", "No customer matches that id or email.");

    public static readonly Error CustomerInactive =
        Error.Validation("enrollment.customer_inactive", "This customer account is deactivated.");

    public static readonly Error AlreadyActive =
        Error.Conflict("enrollment.already_active", "This customer already has active access to this.");

    public static Error NotFound(long id) =>
        Error.NotFound("enrollment.not_found", $"Enrollment {id} was not found.");
}
