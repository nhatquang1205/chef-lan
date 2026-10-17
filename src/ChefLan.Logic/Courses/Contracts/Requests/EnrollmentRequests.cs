using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Requests;

/// <summary>Grant access to a whole course (every lesson). Identify the customer by id OR email, not both.</summary>
public sealed record GrantCourseAccessRequest(
    long CourseId,
    long? CustomerId = null,
    string? CustomerEmail = null,
    bool IsPaid = false,
    decimal? AmountPaidVnd = null,
    string? PaymentNote = null,
    string? PaymentReference = null,
    EnrollmentSource Source = EnrollmentSource.AdminGrant,
    decimal? ListPriceVnd = null,
    decimal? DiscountVnd = null,
    string? DiscountReason = null);

/// <summary>Grant access to a single lesson. The expiry follows the lesson's course.</summary>
public sealed record GrantLessonAccessRequest(
    long LessonId,
    long? CustomerId = null,
    string? CustomerEmail = null,
    bool IsPaid = false,
    decimal? AmountPaidVnd = null,
    string? PaymentNote = null,
    string? PaymentReference = null,
    EnrollmentSource Source = EnrollmentSource.AdminGrant);
