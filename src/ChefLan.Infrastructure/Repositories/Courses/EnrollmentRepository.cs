using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

/// <summary>
/// Both tables are keyed on customer_id, NOT user_id: a customer with no credentials can hold access,
/// which is the point — staff import a Facebook group member and grant access before any login exists.
/// Neither is soft-deleted, so no query filter applies; revoking is status = Revoked. The access check
/// is two single-row lookups, guaranteed by uq_enrollments_customer_id_course_id and
/// uq_lesson_enrollments_customer_id_lesson_id.
/// </summary>
internal sealed class EnrollmentRepository(CoursesDbContext context) : IEnrollmentRepository
{
    public async Task<bool> HasAccessAsync(
        long customerId,
        long courseId,
        long lessonId,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        (await context.Lessons.AnyAsync(l => l.Id == lessonId && l.IncludedInCourse, cancellationToken)
            && await context.Enrollments.AnyAsync(
                e => e.CustomerId == customerId
                    && e.CourseId == courseId
                    && e.Status == EnrollmentStatus.Active
                    && (e.ExpiresAt == null || e.ExpiresAt > utcNow),
                cancellationToken))
        || await context.LessonEnrollments.AnyAsync(
            e => e.CustomerId == customerId
                && e.LessonId == lessonId
                && e.Status == EnrollmentStatus.Active
                && (e.ExpiresAt == null || e.ExpiresAt > utcNow),
            cancellationToken);

    public async Task<(bool HasCourseAccess, IReadOnlyList<long> LessonIds)> GetCourseAccessAsync(
        long customerId,
        long courseId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var hasCourse = await context.Enrollments.AnyAsync(
            e => e.CustomerId == customerId
                && e.CourseId == courseId
                && e.Status == EnrollmentStatus.Active
                && (e.ExpiresAt == null || e.ExpiresAt > utcNow),
            cancellationToken);

        var lessonIds = await context.LessonEnrollments
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId
                && e.Status == EnrollmentStatus.Active
                && (e.ExpiresAt == null || e.ExpiresAt > utcNow)
                && e.Lesson!.CourseId == courseId
                && !e.Lesson.IsDeleted)
            .Select(e => e.LessonId)
            .ToListAsync(cancellationToken);

        return (hasCourse, lessonIds);
    }

    public Task<Enrollment?> GetCourseEnrollmentAsync(long customerId, long courseId, CancellationToken cancellationToken = default) =>
        context.Enrollments.FirstOrDefaultAsync(e => e.CustomerId == customerId && e.CourseId == courseId, cancellationToken);

    public Task<Enrollment?> GetCourseEnrollmentByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<int> CountActiveCourseEnrollmentsAsync(
        long courseId,
        DateTime utcNow,
        long? exceptEnrollmentId,
        CancellationToken cancellationToken = default) =>
        context.Enrollments.CountAsync(
            e => e.CourseId == courseId
                && e.Status == EnrollmentStatus.Active
                && (e.ExpiresAt == null || e.ExpiresAt > utcNow)
                && (exceptEnrollmentId == null || e.Id != exceptEnrollmentId),
            cancellationToken);

    public Task<LessonEnrollment?> GetLessonEnrollmentAsync(long customerId, long lessonId, CancellationToken cancellationToken = default) =>
        context.LessonEnrollments.FirstOrDefaultAsync(e => e.CustomerId == customerId && e.LessonId == lessonId, cancellationToken);

    public Task<LessonEnrollment?> GetLessonEnrollmentByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.LessonEnrollments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Enrollment> Courses, IReadOnlyList<LessonEnrollment> Lessons)> ListForCustomerAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        var courses = await context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.CustomerId == customerId)
            .OrderByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

        var lessons = await context.LessonEnrollments
            .AsNoTracking()
            .Include(e => e.Lesson)
            .Where(e => e.CustomerId == customerId)
            .OrderByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

        return (courses, lessons);
    }

    public async Task<(IReadOnlyList<Enrollment> Courses, IReadOnlyList<LessonEnrollment> Lessons)> ListActiveForCustomerAsync(
        long customerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var courses = await context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.CustomerId == customerId
                && e.Status == EnrollmentStatus.Active
                && (e.ExpiresAt == null || e.ExpiresAt > utcNow)
                && e.Course!.Status == CourseStatus.Published)
            .ToListAsync(cancellationToken);

        var lessons = await context.LessonEnrollments
            .AsNoTracking()
            .Include(e => e.Lesson!).ThenInclude(l => l.Course)
            .Where(e => e.CustomerId == customerId
                && e.Status == EnrollmentStatus.Active
                && (e.ExpiresAt == null || e.ExpiresAt > utcNow)
                && e.Lesson!.Course!.Status == CourseStatus.Published)
            .ToListAsync(cancellationToken);

        return (courses, lessons);
    }

    public void Add(Enrollment enrollment) => context.Enrollments.Add(enrollment);

    public void Add(LessonEnrollment enrollment) => context.LessonEnrollments.Add(enrollment);
}
