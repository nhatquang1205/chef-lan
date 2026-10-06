using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Interfaces;

namespace ChefLan.Infrastructure.Repositories.Courses;

/// <summary>
/// SKELETON — not implemented yet. Arrives with the enrollment endpoints (step 2).
///
/// When implementing, note what this table is keyed on: customer_id, NOT user_id. A
/// customer with no credentials can hold an enrollment, which is the whole point — staff
/// import a Facebook group member and grant access before any login exists. There is no
/// user-to-customer resolution step; a customer token's "sub" IS the customer id.
///
/// GetForCustomerAndCourseAsync is the access check and must stay a single-row lookup —
/// uq_enrollments_customer_id_course_id guarantees that. Enrollments are never soft
/// deleted, so no query filter applies here; revocation is status = Revoked.
/// </summary>
internal sealed class EnrollmentRepository(CoursesDbContext context) : IEnrollmentRepository
{
    public Task<Enrollment?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("EnrollmentRepository.GetByIdAsync — step 2.");

    public Task<Enrollment?> GetForCustomerAndCourseAsync(
        long customerId,
        long courseId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("EnrollmentRepository.GetForCustomerAndCourseAsync — step 2 (access check).");

    public Task<IReadOnlyList<Enrollment>> ListForCustomerAsync(
        long customerId,
        EnrollmentStatus? status = null,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("EnrollmentRepository.ListForCustomerAsync — step 2 (my courses).");

    public Task<PagedResult<Enrollment>> ListForCourseAsync(
        long courseId,
        PagedRequest request,
        EnrollmentStatus? status = null,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("EnrollmentRepository.ListForCourseAsync — step 2 (admin roster).");

    public void Add(Enrollment enrollment) => context.Enrollments.Add(enrollment);
}
