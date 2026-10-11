using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record CourseEnrollmentResponse
{
    public required long Id { get; init; }

    public required long CustomerId { get; init; }

    public required long CourseId { get; init; }

    public string? CourseTitle { get; init; }

    public required EnrollmentStatus Status { get; init; }

    public required EnrollmentSource Source { get; init; }

    public DateTime? AccessGrantedAt { get; init; }

    /// <summary>Null means lifetime.</summary>
    public DateTime? ExpiresAt { get; init; }

    public required bool IsPaid { get; init; }

    public decimal? AmountPaidVnd { get; init; }

    public string? PaymentNote { get; init; }

    public string? PaymentReference { get; init; }
}

public sealed record LessonEnrollmentResponse
{
    public required long Id { get; init; }

    public required long CustomerId { get; init; }

    public required long LessonId { get; init; }

    public long? CourseId { get; init; }

    public string? LessonTitle { get; init; }

    public required EnrollmentStatus Status { get; init; }

    public required EnrollmentSource Source { get; init; }

    public DateTime? AccessGrantedAt { get; init; }

    /// <summary>Null means lifetime.</summary>
    public DateTime? ExpiresAt { get; init; }

    public required bool IsPaid { get; init; }

    public decimal? AmountPaidVnd { get; init; }

    public string? PaymentNote { get; init; }

    public string? PaymentReference { get; init; }
}

public sealed record CustomerAccessResponse(
    long CustomerId,
    IReadOnlyList<CourseEnrollmentResponse> Courses,
    IReadOnlyList<LessonEnrollmentResponse> Lessons);

/// <summary>What the signed-in customer owns within one course, so the course page can unlock the outline.</summary>
public sealed record CourseAccessResponse(bool HasCourseAccess, IReadOnlyList<long> LessonIds);
