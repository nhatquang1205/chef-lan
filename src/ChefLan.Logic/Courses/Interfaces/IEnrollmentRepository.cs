using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Interfaces;

/// <summary>
/// Access to content: a customer may watch a lesson with an active COURSE enrollment (which includes
/// every lesson) or an active LESSON enrollment (just that lesson). Rows point at a customer, not a
/// user, so a customer with no credentials can hold one. Neither table is soft-deleted: revoking is a
/// status change.
/// </summary>
public interface IEnrollmentRepository
{
    /// <summary>
    /// The access check: an active, unexpired course enrollment for the lesson's course OR an active,
    /// unexpired enrollment for that lesson. Two single-row lookups, thanks to the unique indexes.
    /// </summary>
    Task<bool> HasAccessAsync(
        long customerId,
        long courseId,
        long lessonId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>Does the customer own the whole course, and which individual lessons of it?</summary>
    Task<(bool HasCourseAccess, IReadOnlyList<long> LessonIds)> GetCourseAccessAsync(
        long customerId,
        long courseId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked.</summary>
    Task<Enrollment?> GetCourseEnrollmentAsync(long customerId, long courseId, CancellationToken cancellationToken = default);

    /// <summary>Tracked.</summary>
    Task<Enrollment?> GetCourseEnrollmentByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Active, unexpired enrollments of a course, optionally ignoring one row. Feeds the course counter.</summary>
    Task<int> CountActiveCourseEnrollmentsAsync(
        long courseId,
        DateTime utcNow,
        long? exceptEnrollmentId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked.</summary>
    Task<LessonEnrollment?> GetLessonEnrollmentAsync(long customerId, long lessonId, CancellationToken cancellationToken = default);

    /// <summary>Tracked.</summary>
    Task<LessonEnrollment?> GetLessonEnrollmentByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Read-only, with the course / lesson loaded for titles. Newest first.</summary>
    Task<(IReadOnlyList<Enrollment> Courses, IReadOnlyList<LessonEnrollment> Lessons)> ListForCustomerAsync(
        long customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only: the customer's active, unexpired course enrollments (course loaded) and lesson enrollments
    /// (lesson and its course loaded), limited to published courses. Feeds "my courses".
    /// </summary>
    Task<(IReadOnlyList<Enrollment> Courses, IReadOnlyList<LessonEnrollment> Lessons)> ListActiveForCustomerAsync(
        long customerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    void Add(Enrollment enrollment);

    void Add(LessonEnrollment enrollment);
}
