using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;

namespace ChefLan.Logic.Courses.Services;

/// <summary>
/// The single way an order becomes Paid and its access is granted: the webhook, the staff "mark paid" and the
/// staff "link transaction" all come through here. Paying an order that is already Paid does nothing.
///
/// Atomicity: the order and the transaction are tracked on the same DbContext as the grant, so the grant's own
/// SaveChanges commits all three together. Each item is granted by its own SaveChanges, so an order with several items must be wrapped in a transaction by the caller.
/// </summary>
internal sealed class OrderFulfilment(IEnrollmentService enrollments, IUnitOfWork unitOfWork, IClock clock)
{
    /// <returns>Success(true) when the customer already had access (the money is then for staff to refund), Success(false) otherwise.</returns>
    public async Task<Result<bool>> MarkPaidAsync(
        Order order,
        OrderPaidVia via,
        PaymentTransaction? transaction,
        long? staffId,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (order.Status == OrderStatus.Paid)
        {
            return false;
        }

        var now = clock.UtcNow;
        var before = (order.Status, order.PaidAt, order.PaidVia, order.PaidReason, order.PaidByStaffId, order.PaymentTransactionId);

        order.Status = OrderStatus.Paid;
        order.PaidAt = now;
        order.PaidVia = via;
        order.PaidReason = via == OrderPaidVia.Manual ? reason : null;
        order.PaidByStaffId = staffId;
        order.PaymentTransactionId = transaction?.Id;

        var received = transaction?.AmountVnd ?? order.AmountVnd;
        var reference = transaction?.ProviderTransactionId ?? order.Code;
        var note = $"Order {order.Code}";
        var alreadyHadAccess = false;
        var single = order.Items.Count == 1;
        var source = order.PaymentMethod == OrderPaymentMethod.StaffCreated ? EnrollmentSource.AdminGrant : EnrollmentSource.OnlineCheckout;

        foreach (var item in order.Items)
        {
            var failure = item.Kind == OrderItemKind.Course
                ? (await enrollments.GrantCourseAsync(
                    new GrantCourseAccessRequest(
                        item.CourseId!.Value,
                        CustomerId: order.CustomerId,
                        IsPaid: true,
                        AmountPaidVnd: single ? received : item.UnitPriceVnd,
                        PaymentNote: note,
                        PaymentReference: reference,
                        Source: source,
                        ListPriceVnd: item.ListPriceVnd,
                        DiscountVnd: item.DiscountVnd,
                        DiscountReason: item.DiscountReason),
                    staffId,
                    cancellationToken)).ErrorOrNull()
                : (await enrollments.GrantLessonAsync(
                    new GrantLessonAccessRequest(
                        item.LessonId!.Value,
                        CustomerId: order.CustomerId,
                        IsPaid: true,
                        AmountPaidVnd: single ? received : item.UnitPriceVnd,
                        PaymentNote: note,
                        PaymentReference: reference,
                        Source: source),
                    staffId,
                    cancellationToken)).ErrorOrNull();

            if (failure is null)
            {
                continue;
            }

            // Already owned (a race with a staff grant): the order is still paid, the money is now a refund case.
            if (failure.Code == EnrollmentErrors.AlreadyActive.Code)
            {
                alreadyHadAccess = true;
                continue;
            }

            // Nothing was saved: put the order back so the caller's unit of work does not persist a half-paid order.
            (order.Status, order.PaidAt, order.PaidVia, order.PaidReason, order.PaidByStaffId, order.PaymentTransactionId) = before;

            return failure;
        }

        // A grant saves the order with it. This covers the already-owned path, where no grant saved anything.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return alreadyHadAccess;
    }
}

internal static class ResultExtensions
{
    public static Error? ErrorOrNull<T>(this Result<T> result) => result.IsFailure ? result.Error : null;
}
