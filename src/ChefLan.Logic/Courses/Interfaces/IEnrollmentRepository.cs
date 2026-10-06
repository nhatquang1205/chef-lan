using ChefLan.Commons.Pagination;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Interfaces;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The access check. A single-row lookup thanks to uq_enrollments_customer_id_course_id.
    /// </summary>
    Task<Enrollment?> GetForCustomerAndCourseAsync(
        long customerId,
        long courseId,
        CancellationToken cancellationToken = default);

    /// <summary>"My courses" — includes the Course navigation.</summary>
    Task<IReadOnlyList<Enrollment>> ListForCustomerAsync(
        long customerId,
        EnrollmentStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>The admin roster for one course.</summary>
    Task<PagedResult<Enrollment>> ListForCourseAsync(
        long courseId,
        PagedRequest request,
        EnrollmentStatus? status = null,
        CancellationToken cancellationToken = default);

    void Add(Enrollment enrollment);
}
