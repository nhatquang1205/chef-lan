using ChefLan.Commons.Money;
using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;
using ChefLan.Logic.Courses.Payments;

namespace ChefLan.Logic.Courses.Services;

internal sealed class OrderService(
    IOrderRepository orders,
    IPaymentTransactionRepository transactions,
    ICourseRepository courses,
    ILessonRepository lessons,
    IEnrollmentRepository enrollmentRepository,
    IEnrollmentService enrollments,
    ICustomerDirectory customers,
    IPaymentSettings settings,
    OrderFulfilment fulfilment,
    IUnitOfWork unitOfWork,
    IClock clock) : IOrderService
{
    private const int MaxCodeAttempts = 5;
    private const int MyOrdersLimit = 50;

    public CheckoutConfigResponse GetConfig() => new(settings.CheckoutEnabled);

    public async Task<Result<OrderResponse>> CreateAsync(
        long customerId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!settings.CheckoutEnabled)
        {
            return OrderErrors.CheckoutDisabled;
        }

        if (string.IsNullOrWhiteSpace(request.CourseSlug) == (request.LessonId is null))
        {
            return OrderErrors.ItemRequired;
        }

        var customer = await customers.FindAsync(customerId, null, cancellationToken);

        if (customer is null)
        {
            return EnrollmentErrors.CustomerNotFound;
        }

        if (!customer.IsActive)
        {
            return EnrollmentErrors.CustomerInactive;
        }

        var now = clock.UtcNow;
        var item = await BuildItemAsync(customerId, request, now, cancellationToken);

        if (item.IsFailure)
        {
            return item.Error;
        }

        var kind = item.Value.Kind;
        var targetId = kind == OrderItemKind.Course ? item.Value.CourseId!.Value : item.Value.LessonId!.Value;

        // The same item at the same price: hand back the order the customer already has.
        var existing = await orders.FindPendingForItemAsync(customerId, kind, targetId, now, cancellationToken);

        if (existing is not null)
        {
            if (existing.AmountVnd == item.Value.UnitPriceVnd)
            {
                return existing.ToResponse(settings, now);
            }

            // The price moved since: the old QR would ask for the wrong amount.
            existing.Status = OrderStatus.Cancelled;
        }

        var order = new Order
        {
            Code = await NewCodeAsync(cancellationToken),
            CustomerId = customerId,
            AmountVnd = item.Value.UnitPriceVnd,
            ExpiresAt = now + settings.OrderLifetime,
            Items = [item.Value],
        };

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToResponse(settings, now);
    }

    public async Task<Result<OrderResponse>> GetAsync(long customerId, string code, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByCodeAsync(OrderCodes.Normalize(code), cancellationToken);

        // Someone else's order looks exactly like a missing one.
        return order is null || order.CustomerId != customerId
            ? OrderErrors.NotFound(code)
            : order.ToResponse(settings, clock.UtcNow);
    }

    public async Task<Result<OrderResponse>> CancelAsync(long customerId, string code, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByCodeAsync(OrderCodes.Normalize(code), cancellationToken);

        if (order is null || order.CustomerId != customerId)
        {
            return OrderErrors.NotFound(code);
        }

        var now = clock.UtcNow;

        if (order.EffectiveStatus(now) != OrderStatus.Pending)
        {
            return OrderErrors.NotPending;
        }

        order.Status = OrderStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToResponse(settings, now);
    }

    public async Task<IReadOnlyList<OrderResponse>> ListMineAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var rows = await orders.ListForCustomerAsync(customerId, MyOrdersLimit, cancellationToken);

        return rows.Select(o => o.ToResponse(settings, now)).ToList();
    }

    public async Task<PagedResult<AdminOrderResponse>> ListAsync(
        OrderStatus? status,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var page = await orders.ListAsync(status, paging, cancellationToken);

        return PagedResult<AdminOrderResponse>.Create(
            page.Items.Select(o => o.ToAdminResponse(now)).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<Result<AdminOrderResponse>> MarkPaidAsync(
        long orderId,
        MarkOrderPaidRequest request,
        long? staffId,
        CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.NotFound(orderId);
        }

        if (order.Status == OrderStatus.Paid)
        {
            return OrderErrors.AlreadyPaid;
        }

        PaymentTransaction? transaction = null;

        if (request.PaymentTransactionId is { } transactionId)
        {
            transaction = await transactions.GetByIdAsync(transactionId, cancellationToken);

            if (transaction is null)
            {
                return OrderErrors.TransactionNotFound(transactionId);
            }

            if (transaction.OrderId is not null && transaction.OrderId != order.Id)
            {
                return OrderErrors.TransactionLinked;
            }
        }

        var result = await fulfilment.MarkPaidAsync(order, OrderPaidVia.Manual, transaction, staffId, request.Reason.Trim(), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        if (transaction is not null)
        {
            Reconcile(transaction, order, result.Value, "Linked by staff while marking the order paid.");
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return order.ToAdminResponse(clock.UtcNow);
    }

    public async Task<Result<AdminOrderResponse>> CancelAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.NotFound(orderId);
        }

        var now = clock.UtcNow;

        if (order.EffectiveStatus(now) != OrderStatus.Pending)
        {
            return OrderErrors.NotPending;
        }

        order.Status = OrderStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToAdminResponse(now);
    }

    /// <summary>Records on a transaction what staff did with it.</summary>
    internal static void Reconcile(PaymentTransaction transaction, Order order, bool customerAlreadyHadAccess, string note)
    {
        transaction.OrderId = order.Id;
        transaction.Code ??= order.Code;
        transaction.MatchStatus = customerAlreadyHadAccess
            ? PaymentMatchStatus.Duplicate
            : transaction.AmountVnd > order.AmountVnd ? PaymentMatchStatus.Overpaid : PaymentMatchStatus.Matched;
        transaction.Note = customerAlreadyHadAccess ? $"{note} The customer already had access: refund." : note;
    }

    private async Task<Result<OrderItem>> BuildItemAsync(
        long customerId,
        CreateOrderRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.CourseSlug))
        {
            var courseId = await courses.GetPublishedIdBySlugAsync(request.CourseSlug.Trim(), cancellationToken);

            if (courseId is null)
            {
                return CourseErrors.NotFound(request.CourseSlug);
            }

            var course = await courses.GetByIdAsync(courseId.Value, cancellationToken);

            if (course is null)
            {
                return CourseErrors.NotFound(courseId.Value);
            }

            var owned = await enrollmentRepository.OwnedCourseIdsAsync(customerId, [course.Id], now, cancellationToken);

            if (owned.Contains(course.Id))
            {
                return OrderErrors.AlreadyOwned;
            }

            var quote = await enrollments.QuoteCourseAsync(course.Id, customerId, cancellationToken);

            if (quote.IsFailure)
            {
                return quote.Error;
            }

            if (!Vnd.IsValid(quote.Value.FinalPriceVnd) || quote.Value.FinalPriceVnd <= 0)
            {
                return OrderErrors.NotForSale;
            }

            return new OrderItem
            {
                Kind = OrderItemKind.Course,
                CourseId = course.Id,
                Title = course.Title,
                CourseSlug = course.Slug,
                ListPriceVnd = quote.Value.ListPriceVnd,
                DiscountVnd = quote.Value.ListPriceVnd - quote.Value.FinalPriceVnd,
                DiscountReason = quote.Value.Discounts.Count == 0 ? null : string.Join("; ", quote.Value.Discounts.Select(d => d.Reason)),
                UnitPriceVnd = quote.Value.FinalPriceVnd,
            };
        }

        var lesson = await lessons.GetByIdAsync(request.LessonId!.Value, cancellationToken);
        var lessonCourse = lesson is null ? null : await courses.GetByIdAsync(lesson.CourseId, cancellationToken);

        // Not found, unpublished and not-for-sale all look the same from outside.
        if (lesson is null || lessonCourse is null || !lesson.IsPublished || lessonCourse.Status != CourseStatus.Published)
        {
            return LessonErrors.NotFound(request.LessonId.Value);
        }

        if (lesson.PriceVnd is not { } price || !Vnd.IsValid(price) || price <= 0)
        {
            return OrderErrors.NotForSale;
        }

        if (await enrollmentRepository.HasAccessAsync(customerId, lesson.CourseId, lesson.Id, now, cancellationToken))
        {
            return OrderErrors.AlreadyOwned;
        }

        return new OrderItem
        {
            Kind = OrderItemKind.Lesson,
            LessonId = lesson.Id,
            Title = lesson.Title,
            CourseSlug = lessonCourse.Slug,
            ListPriceVnd = price,
            DiscountVnd = 0,
            UnitPriceVnd = price,
        };
    }

    private async Task<string> NewCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var code = OrderCodes.Generate(settings.CodePrefix);

            if (!await orders.CodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        // 32^6 codes: reaching this means something is badly wrong, and a unique index would catch it anyway.
        throw new InvalidOperationException("Could not generate a free order code.");
    }
}
