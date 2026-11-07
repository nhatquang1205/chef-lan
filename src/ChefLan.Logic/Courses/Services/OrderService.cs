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
    ICourseIncludeRepository includes,
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
        if (request.Method == OrderPaymentMethod.BankTransfer && !settings.CheckoutEnabled)
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
                if (existing.PaymentMethod != request.Method)
                {
                    existing.PaymentMethod = request.Method;
                    existing.ExpiresAt = now + LifetimeOf(request.Method);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

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
            PaymentMethod = request.Method,
            ExpiresAt = now + LifetimeOf(request.Method),
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
        AdminOrderFilter filter,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var page = await orders.ListAsync(filter, now, paging, cancellationToken);

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

    public async Task<Result<StaffOrderQuoteResponse>> QuoteForStaffAsync(
        StaffOrderQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await PlanAsync(request.CustomerId, request.CourseIds, cancellationToken);

        if (plan.IsFailure)
        {
            return plan.Error;
        }

        var items = request.CourseIds.Select(id =>
        {
            var course = plan.Value.Courses[id];
            var quoted = plan.Value.Quoted.GetValueOrDefault(id);
            var owned = quoted is null;

            return new StaffQuoteItemResponse(
                course.Id,
                course.Title,
                course.Slug,
                course.Status,
                course.PriceVnd,
                owned ? [] : quoted!.Discounts.Select(d => new QuoteDiscountResponse(d.IncludedCourseId, d.Title, d.AmountVnd, d.Reason)).ToList(),
                owned ? course.PriceVnd : quoted!.SystemPriceVnd,
                owned);
        }).ToList();

        return new StaffOrderQuoteResponse(items, items.Where(i => !i.AlreadyOwned).Sum(i => i.SystemPriceVnd));
    }

    public async Task<Result<AdminOrderResponse>> CreateByStaffAsync(
        CreateStaffOrderRequest request,
        long? staffId,
        CancellationToken cancellationToken = default)
    {
        var customer = await customers.FindAsync(request.CustomerId, null, cancellationToken);

        if (customer is null)
        {
            return EnrollmentErrors.CustomerNotFound;
        }

        if (!customer.IsActive)
        {
            return EnrollmentErrors.CustomerInactive;
        }

        var plan = await PlanAsync(request.CustomerId, request.Items.Select(i => i.CourseId).ToList(), cancellationToken);

        if (plan.IsFailure)
        {
            return plan.Error;
        }

        foreach (var item in request.Items)
        {
            if (!plan.Value.Quoted.ContainsKey(item.CourseId))
            {
                return OrderErrors.AlreadyOwnedCourse(plan.Value.Courses[item.CourseId].Title);
            }
        }

        var edits = new Dictionary<long, decimal>();

        foreach (var item in request.Items.Where(i => i.UnitPriceVnd is not null))
        {
            var course = plan.Value.Courses[item.CourseId];
            var price = item.UnitPriceVnd!.Value;

            if (!Vnd.IsValid(price) || price > course.PriceVnd)
            {
                return OrderErrors.PriceOutOfRange(course.Title);
            }

            edits[item.CourseId] = price;
        }

        var discount = request.OrderDiscountVnd ?? 0;
        var quoted = request.Items.Select(i => plan.Value.Quoted[i.CourseId]).ToList();
        var afterEdits = quoted.Sum(q => edits.GetValueOrDefault(q.CourseId, q.SystemPriceVnd));

        if (!Vnd.IsValid(discount) || discount > afterEdits)
        {
            return OrderErrors.DiscountTooLarge;
        }

        var priced = OrderQuote.Price(quoted, edits, discount, request.OrderDiscountReason);
        var total = priced.Sum(p => p.UnitPriceVnd);

        if (total <= 0)
        {
            return OrderErrors.TotalNotPositive;
        }

        var now = clock.UtcNow;

        var order = new Order
        {
            Code = await NewCodeAsync(cancellationToken),
            CustomerId = request.CustomerId,
            PaymentMethod = OrderPaymentMethod.StaffCreated,
            AmountVnd = total,
            DiscountVnd = discount > 0 ? discount : null,
            DiscountReason = discount > 0 ? request.OrderDiscountReason!.Trim() : null,
            ExpiresAt = now,
            Items = priced.Select(p =>
            {
                var course = plan.Value.Courses[p.CourseId];

                return new OrderItem
                {
                    Kind = OrderItemKind.Course,
                    CourseId = course.Id,
                    Title = course.Title,
                    CourseSlug = course.Slug,
                    ListPriceVnd = p.ListPriceVnd,
                    DiscountVnd = p.DiscountVnd,
                    DiscountReason = p.DiscountReason,
                    UnitPriceVnd = p.UnitPriceVnd,
                };
            }).ToList(),
        };

        var saved = await unitOfWork.InTransactionAsync<Order>(async () =>
        {
            orders.Add(order);

            var paid = await fulfilment.MarkPaidAsync(order, OrderPaidVia.Manual, null, staffId, request.Note.Trim(), cancellationToken);

            // Owning one of them by now means a grant raced this order: undo it all rather than keep a paid order with no grant.
            if (paid.IsFailure)
            {
                return paid.Error;
            }

            return paid.Value ? EnrollmentErrors.AlreadyActive : order;
        }, cancellationToken);

        if (saved.IsFailure)
        {
            return saved.Error;
        }

        // Loaded again so the response carries the customer's name and contact.
        return (await orders.GetByIdAsync(order.Id, cancellationToken) ?? order).ToAdminResponse(now);
    }

    private async Task<Result<StaffPlan>> PlanAsync(
        long customerId,
        IReadOnlyList<long> courseIds,
        CancellationToken cancellationToken)
    {
        var customer = await customers.FindAsync(customerId, null, cancellationToken);

        if (customer is null)
        {
            return EnrollmentErrors.CustomerNotFound;
        }

        var now = clock.UtcNow;
        var loaded = new Dictionary<long, Course>();

        foreach (var id in courseIds)
        {
            var course = await courses.GetByIdAsync(id, cancellationToken);

            if (course is null)
            {
                return CourseErrors.NotFound(id);
            }

            loaded[id] = course;
        }

        var rows = new List<CourseInclude>();

        foreach (var id in courseIds)
        {
            rows.AddRange(await includes.ListForCourseAsync(id, cancellationToken));
        }

        var moduleIds = rows.Select(r => r.IncludedCourseId).Distinct();
        var owned = await enrollmentRepository.OwnedCourseIdsAsync(customerId, courseIds.Concat(moduleIds).Distinct().ToList(), now, cancellationToken);

        var toPrice = courseIds.Where(id => !owned.Contains(id)).Select(id => loaded[id]).ToList();

        var quoted = OrderQuote.Quote(
            toPrice.Select(c => new QuoteCourse(c.Id, c.Title, c.PriceVnd)).ToList(),
            rows.Select(r => new QuoteInclude(r.CourseId, r.IncludedCourseId, r.IncludedCourse?.Title ?? string.Empty, r.DiscountVnd, r.SortOrder)).ToList(),
            owned);

        return new StaffPlan(loaded, quoted.ToDictionary(q => q.CourseId));
    }

    /// <summary>The courses asked for, and the priced ones among them (a course the customer owns has no entry).</summary>
    private sealed record StaffPlan(Dictionary<long, Course> Courses, Dictionary<long, QuotedCourse> Quoted);

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

    private TimeSpan LifetimeOf(OrderPaymentMethod method) =>
        method == OrderPaymentMethod.ContactChef ? settings.ContactOrderLifetime : settings.OrderLifetime;

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
