using ChefLan.Commons.Money;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;

namespace ChefLan.Logic.Courses.Services;

internal sealed class EnrollmentService(
    IEnrollmentRepository enrollments,
    ICourseRepository courses,
    ILessonRepository lessons,
    ICourseIncludeRepository includes,
    ICustomerDirectory customers,
    IUnitOfWork unitOfWork,
    IClock clock) : IEnrollmentService
{
    public async Task<Result<CourseEnrollmentResponse>> GrantCourseAsync(
        GrantCourseAccessRequest request,
        long? staffId,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(request.CustomerId, request.CustomerEmail, cancellationToken);

        if (customer.IsFailure)
        {
            return customer.Error;
        }

        var course = await courses.GetByIdAsync(request.CourseId, cancellationToken);

        if (course is null)
        {
            return CourseErrors.NotFound(request.CourseId);
        }

        var now = clock.UtcNow;
        var enrollment = await enrollments.GetCourseEnrollmentAsync(customer.Value.Id, course.Id, cancellationToken);

        if (enrollment is not null && enrollment.IsActiveAt(now))
        {
            return EnrollmentErrors.AlreadyActive;
        }

        var isNew = enrollment is null;
        enrollment ??= new Enrollment { CustomerId = customer.Value.Id, CourseId = course.Id };

        Apply(enrollment, request.Source, request.IsPaid, request.AmountPaidVnd, request.PaymentNote, request.PaymentReference, staffId, now, course.AccessDurationDays);

        if (isNew)
        {
            enrollments.Add(enrollment);
        }

        // The course's denormalized counter, in the same SaveChanges as the grant.
        course.EnrollmentCount =
            await enrollments.CountActiveCourseEnrollmentsAsync(course.Id, now, enrollment.Id == 0 ? null : enrollment.Id, cancellationToken) + 1;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(enrollment, course.Title);
    }

    public async Task<Result<LessonEnrollmentResponse>> GrantLessonAsync(
        GrantLessonAccessRequest request,
        long? staffId,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(request.CustomerId, request.CustomerEmail, cancellationToken);

        if (customer.IsFailure)
        {
            return customer.Error;
        }

        var lesson = await lessons.GetByIdAsync(request.LessonId, cancellationToken);

        if (lesson is null)
        {
            return LessonErrors.NotFound(request.LessonId);
        }

        // "Same rule as the course": the lesson's access period is its course's.
        var course = await courses.GetByIdAsync(lesson.CourseId, cancellationToken);

        if (course is null)
        {
            return CourseErrors.NotFound(lesson.CourseId);
        }

        var now = clock.UtcNow;
        var enrollment = await enrollments.GetLessonEnrollmentAsync(customer.Value.Id, lesson.Id, cancellationToken);

        if (enrollment is not null && enrollment.IsActiveAt(now))
        {
            return EnrollmentErrors.AlreadyActive;
        }

        var isNew = enrollment is null;
        enrollment ??= new LessonEnrollment { CustomerId = customer.Value.Id, LessonId = lesson.Id };

        enrollment.Status = EnrollmentStatus.Active;
        enrollment.Source = request.Source;
        enrollment.AccessGrantedAt = now;
        enrollment.ExpiresAt = course.AccessDurationDays is { } days ? now.AddDays(days) : null;
        enrollment.IsPaid = request.IsPaid;
        enrollment.PaidAt = request.IsPaid ? now : null;
        enrollment.AmountPaidVnd = request.IsPaid ? request.AmountPaidVnd : null;
        enrollment.PaymentNote = SlugAllocator.Clean(request.PaymentNote);
        enrollment.PaymentReference = SlugAllocator.Clean(request.PaymentReference);
        enrollment.GrantedByStaffId = staffId;

        if (isNew)
        {
            enrollments.Add(enrollment);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(enrollment, lesson.Title, lesson.CourseId);
    }

    public async Task<Result> RevokeCourseAsync(long enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await enrollments.GetCourseEnrollmentByIdAsync(enrollmentId, cancellationToken);

        if (enrollment is null)
        {
            return Result.Failure(EnrollmentErrors.NotFound(enrollmentId));
        }

        if (enrollment.Status != EnrollmentStatus.Revoked)
        {
            enrollment.Status = EnrollmentStatus.Revoked;

            var course = await courses.GetByIdAsync(enrollment.CourseId, cancellationToken);

            if (course is not null)
            {
                course.EnrollmentCount = await enrollments.CountActiveCourseEnrollmentsAsync(
                    course.Id, clock.UtcNow, enrollment.Id, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> RevokeLessonAsync(long enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await enrollments.GetLessonEnrollmentByIdAsync(enrollmentId, cancellationToken);

        if (enrollment is null)
        {
            return Result.Failure(EnrollmentErrors.NotFound(enrollmentId));
        }

        if (enrollment.Status != EnrollmentStatus.Revoked)
        {
            enrollment.Status = EnrollmentStatus.Revoked;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<CustomerAccessResponse>> GetCustomerAccessAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await customers.FindAsync(customerId, null, cancellationToken);

        if (customer is null)
        {
            return EnrollmentErrors.CustomerNotFound;
        }

        var (courseRows, lessonRows) = await enrollments.ListForCustomerAsync(customerId, cancellationToken);

        return new CustomerAccessResponse(
            customerId,
            courseRows.Select(e => ToResponse(e, e.Course?.Title)).ToList(),
            lessonRows.Select(e => ToResponse(e, e.Lesson?.Title, e.Lesson?.CourseId)).ToList());
    }

    public async Task<IReadOnlyList<MyCourseResponse>> ListMyCoursesAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        var (courseRows, lessonRows) = await enrollments.ListActiveForCustomerAsync(customerId, clock.UtcNow, cancellationToken);

        var courseIds = courseRows.Select(e => e.CourseId)
            .Concat(lessonRows.Select(e => e.Lesson!.CourseId))
            .Distinct();

        return courseIds
            .Select(courseId =>
            {
                var whole = courseRows.FirstOrDefault(e => e.CourseId == courseId);
                var owned = lessonRows.Where(e => e.Lesson!.CourseId == courseId).ToList();
                var course = whole?.Course ?? owned[0].Lesson!.Course!;

                return (
                    LastGrant: owned.Select(e => e.AccessGrantedAt).Append(whole?.AccessGrantedAt).Max(),
                    Item: new MyCourseResponse
                    {
                        CourseId = courseId,
                        Slug = course.Slug,
                        Title = course.Title,
                        ThumbnailUrl = course.ThumbnailUrl,
                        HasCourseAccess = whole is not null,
                        CourseExpiresAt = whole?.ExpiresAt,
                        Lessons = owned
                            .OrderBy(e => e.Lesson!.SortOrder)
                            .Select(e => new MyLessonResponse(e.LessonId, e.Lesson!.Title, e.ExpiresAt))
                            .ToList(),
                    });
            })
            .OrderByDescending(x => x.LastGrant)
            .Select(x => x.Item)
            .ToList();
    }

    public async Task<Result<CourseAccessResponse>> GetStaffCourseAccessAsync(
        string courseSlug,
        CancellationToken cancellationToken = default)
    {
        var courseId = await courses.GetPublishedIdBySlugAsync(courseSlug, cancellationToken);

        if (courseId is null)
        {
            return CourseErrors.NotFound(courseSlug);
        }

        var published = await lessons.ListForCourseAsync(courseId.Value, includeUnpublished: false, cancellationToken);
        var included = await includes.ListPublishedIncludedWithLessonsAsync(courseId.Value, cancellationToken);

        return new CourseAccessResponse(true, published.Select(l => l.Id).ToList())
        {
            ViewerIsStaff = true,
            IncludedCourses = included
                .Select(c => new IncludedCourseAccessResponse(c.Slug, true, c.Lessons.Select(l => l.Id).ToList()))
                .ToList(),
        };
    }

    public async Task<Result<CourseAccessResponse>> GetCourseAccessAsync(
        string courseSlug,
        long customerId,
        CancellationToken cancellationToken = default)
    {
        var courseId = await courses.GetPublishedIdBySlugAsync(courseSlug, cancellationToken);

        if (courseId is null)
        {
            return CourseErrors.NotFound(courseSlug);
        }

        var now = clock.UtcNow;
        var (hasCourse, lessonIds) = await enrollments.GetCourseAccessAsync(customerId, courseId.Value, now, cancellationToken);
        var included = await includes.ListPublishedIncludedWithLessonsAsync(courseId.Value, cancellationToken);
        var owned = await enrollments.OwnedCourseIdsAsync(customerId, included.Select(c => c.Id).ToList(), now, cancellationToken);

        var includedAccess = new List<IncludedCourseAccessResponse>(included.Count);

        foreach (var course in included)
        {
            var (_, grants) = await enrollments.GetCourseAccessAsync(customerId, course.Id, now, cancellationToken);

            includedAccess.Add(new IncludedCourseAccessResponse(
                course.Slug,
                hasCourse || owned.Contains(course.Id),
                grants));
        }

        return new CourseAccessResponse(hasCourse, lessonIds) { IncludedCourses = includedAccess };
    }

    private async Task<Result<CustomerRef>> ResolveCustomerAsync(
        long? customerId,
        string? email,
        CancellationToken cancellationToken)
    {
        if ((customerId is not null) == !string.IsNullOrWhiteSpace(email))
        {
            return EnrollmentErrors.CustomerRequired;
        }

        var customer = await customers.FindAsync(customerId, email?.Trim(), cancellationToken);

        if (customer is null)
        {
            return EnrollmentErrors.CustomerNotFound;
        }

        return customer.IsActive ? customer : EnrollmentErrors.CustomerInactive;
    }

    private static void Apply(
        Enrollment enrollment,
        EnrollmentSource source,
        bool isPaid,
        decimal? amount,
        string? note,
        string? reference,
        long? staffId,
        DateTime now,
        int? accessDurationDays)
    {
        enrollment.Status = EnrollmentStatus.Active;
        enrollment.Source = source;
        enrollment.AccessGrantedAt = now;

        // Snapshotted, so editing the course's access period later never changes existing students.
        enrollment.ExpiresAt = accessDurationDays is { } days ? now.AddDays(days) : null;
        enrollment.IsPaid = isPaid;
        enrollment.PaidAt = isPaid ? now : null;
        enrollment.AmountPaidVnd = isPaid ? amount : null;
        enrollment.PaymentNote = SlugAllocator.Clean(note);
        enrollment.PaymentReference = SlugAllocator.Clean(reference);
        enrollment.GrantedByStaffId = staffId;
    }

    private static CourseEnrollmentResponse ToResponse(Enrollment e, string? courseTitle) => new()
    {
        Id = e.Id,
        CustomerId = e.CustomerId,
        CourseId = e.CourseId,
        CourseTitle = courseTitle,
        Status = e.Status,
        Source = e.Source,
        AccessGrantedAt = e.AccessGrantedAt,
        ExpiresAt = e.ExpiresAt,
        IsPaid = e.IsPaid,
        AmountPaidVnd = e.AmountPaidVnd,
        PaymentNote = e.PaymentNote,
        PaymentReference = e.PaymentReference,
    };

    private static LessonEnrollmentResponse ToResponse(LessonEnrollment e, string? lessonTitle, long? courseId) => new()
    {
        Id = e.Id,
        CustomerId = e.CustomerId,
        LessonId = e.LessonId,
        CourseId = courseId,
        LessonTitle = lessonTitle,
        Status = e.Status,
        Source = e.Source,
        AccessGrantedAt = e.AccessGrantedAt,
        ExpiresAt = e.ExpiresAt,
        IsPaid = e.IsPaid,
        AmountPaidVnd = e.AmountPaidVnd,
        PaymentNote = e.PaymentNote,
        PaymentReference = e.PaymentReference,
    };
}
