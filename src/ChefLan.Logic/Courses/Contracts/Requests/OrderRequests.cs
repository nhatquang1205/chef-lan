namespace ChefLan.Logic.Courses.Contracts.Requests;

/// <summary>Buy a course (by slug) or one lesson (by id). Exactly one of the two.</summary>
public sealed record CreateOrderRequest(string? CourseSlug = null, long? LessonId = null);

/// <summary>Staff confirm a payment by hand. A reason is always required; a bank transaction is optional.</summary>
public sealed record MarkOrderPaidRequest(string Reason, long? PaymentTransactionId = null);

/// <summary>Staff attach a bank transaction to an order.</summary>
public sealed record LinkPaymentRequest(long OrderId);
