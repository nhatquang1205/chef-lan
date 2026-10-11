using ChefLan.Commons.Results;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

public interface IEnrollmentService
{
    /// <summary>
    /// Grants access to a whole course. A revoked or expired row is reactivated (and its expiry restarted);
    /// an enrollment that is still active is a 409, so a double grant cannot double-count a payment.
    /// </summary>
    Task<Result<CourseEnrollmentResponse>> GrantCourseAsync(
        GrantCourseAccessRequest request, long? staffId, CancellationToken cancellationToken = default);

    Task<Result<LessonEnrollmentResponse>> GrantLessonAsync(
        GrantLessonAccessRequest request, long? staffId, CancellationToken cancellationToken = default);

    Task<Result> RevokeCourseAsync(long enrollmentId, CancellationToken cancellationToken = default);

    Task<Result> RevokeLessonAsync(long enrollmentId, CancellationToken cancellationToken = default);

    Task<Result<CustomerAccessResponse>> GetCustomerAccessAsync(long customerId, CancellationToken cancellationToken = default);

    /// <summary>For a signed-in customer: whole-course ownership and owned lessons for one published course.</summary>
    Task<Result<CourseAccessResponse>> GetCourseAccessAsync(
        string courseSlug, long customerId, CancellationToken cancellationToken = default);
}
