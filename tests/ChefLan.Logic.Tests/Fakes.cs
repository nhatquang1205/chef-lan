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

namespace ChefLan.Logic.Tests;

/// <summary>Everything the payment flow talks to, in memory, with ids assigned on save like the database would.</summary>
internal sealed class FakeWorld
{
    public DateTime Now { get; set; } = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    public List<Order> Orders { get; } = [];

    public List<PaymentTransaction> Transactions { get; } = [];

    public List<GrantCourseAccessRequest> CourseGrants { get; } = [];

    public List<GrantLessonAccessRequest> LessonGrants { get; } = [];

    /// <summary>When true a grant answers "already active", as when the customer already owns the course.</summary>
    public bool CustomerAlreadyOwns { get; set; }

    /// <summary>Makes the next transaction insert fail as if another request stored the same notification first.</summary>
    public bool NextInsertLosesRace { get; set; }

    public decimal QuotePrice { get; set; } = 1_000_000;

    public decimal CoursePrice { get; set; } = 1_000_000;

    private long _nextId = 1;

    public FakeSettings Settings { get; } = new();

    public Order AddPendingOrder(decimal amount = 1_000_000, string code = "BB7K2M9Q", int expiresInMinutes = 1440, bool lesson = false)
    {
        var order = new Order
        {
            Id = _nextId++,
            Code = code,
            CustomerId = 5,
            AmountVnd = amount,
            ExpiresAt = Now.AddMinutes(expiresInMinutes),
            CreatedAt = Now,
            Items =
            [
                new OrderItem
                {
                    Kind = lesson ? OrderItemKind.Lesson : OrderItemKind.Course,
                    CourseId = lesson ? null : 10,
                    LessonId = lesson ? 20 : null,
                    Title = "Macaron",
                    CourseSlug = "macaron",
                    ListPriceVnd = amount + 100_000,
                    DiscountVnd = 100_000,
                    UnitPriceVnd = amount,
                },
            ],
        };

        Orders.Add(order);

        return order;
    }

    public PaymentHarness Payments() => new(this);

    public long NextId() => _nextId++;
}

internal sealed class FakeSettings : IPaymentSettings
{
    public string Provider => "SePay";

    public bool CheckoutEnabled { get; set; } = true;

    public bool SePayConfigured { get; set; } = true;

    public string MerchantId => "SP-TEST-1";

    public string SecretKey => "test-secret";

    public string CheckoutUrl => "https://pay.example/v1/checkout/init";

    public string SiteBaseUrl => "https://shop.example";

    public string CodePrefix => "BB";

    public TimeSpan OrderLifetime => TimeSpan.FromHours(24);

    public TimeSpan ContactOrderLifetime => TimeSpan.FromDays(7);

    public bool VerifyIpnSecret(string? presented) => presented == "ipn-secret";
}

internal sealed class FakeClock(FakeWorld world) : IClock
{
    public DateTime UtcNow => world.Now;
}

internal sealed class FakeUnitOfWork(FakeWorld world) : IUnitOfWork
{
    public Task<Result<T>> InTransactionAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken = default) => work();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var order in world.Orders.Where(o => o.Id == 0))
        {
            order.Id = world.NextId();
        }

        foreach (var transaction in world.Transactions.Where(t => t.Id == 0))
        {
            if (world.NextInsertLosesRace)
            {
                world.NextInsertLosesRace = false;
                world.Transactions.Remove(transaction);
                world.Transactions.Add(new PaymentTransaction
                {
                    Id = world.NextId(),
                    Provider = transaction.Provider,
                    ProviderTransactionId = transaction.ProviderTransactionId,
                    AmountVnd = transaction.AmountVnd,
                    MatchStatus = PaymentMatchStatus.Unmatched,
                });

                throw new ConflictException("duplicate");
            }

            transaction.Id = world.NextId();
            transaction.CreatedAt = world.Now;
        }

        return Task.FromResult(1);
    }
}

internal sealed class FakeOrderRepository(FakeWorld world) : IOrderRepository
{
    public Task<Order?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(world.Orders.FirstOrDefault(o => o.Code == code));

    public Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(world.Orders.FirstOrDefault(o => o.Id == id));

    public Task<Order?> FindPendingForItemAsync(long customerId, OrderItemKind kind, long targetId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult(world.Orders.FirstOrDefault(o => o.CustomerId == customerId
            && o.Status == OrderStatus.Pending
            && o.ExpiresAt > utcNow
            && o.Items.Any(i => i.Kind == kind && (i.CourseId == targetId || i.LessonId == targetId))));

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(world.Orders.Any(o => o.Code == code));

    public Task<IReadOnlyList<Order>> ListForCustomerAsync(long customerId, int take, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<PagedResult<Order>> ListAsync(AdminOrderFilter filter, DateTime utcNow, PagedRequest paging, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public void Add(Order order) => world.Orders.Add(order);
}

internal sealed class FakeTransactionRepository(FakeWorld world) : IPaymentTransactionRepository
{
    public Task<PaymentTransaction?> GetByProviderIdAsync(string provider, string providerTransactionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(world.Transactions.FirstOrDefault(t => t.Provider == provider && t.ProviderTransactionId == providerTransactionId));

    public Task<PaymentTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(world.Transactions.FirstOrDefault(t => t.Id == id));

    public Task<PagedResult<PaymentTransaction>> ListAsync(IReadOnlyCollection<PaymentMatchStatus>? statuses, PagedRequest paging, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public void Add(PaymentTransaction transaction) => world.Transactions.Add(transaction);
}

internal sealed class FakeEnrollmentService(FakeWorld world) : IEnrollmentService
{
    public Task<Result<CourseEnrollmentResponse>> GrantCourseAsync(GrantCourseAccessRequest request, long? staffId, CancellationToken cancellationToken = default)
    {
        if (world.CustomerAlreadyOwns)
        {
            return Task.FromResult<Result<CourseEnrollmentResponse>>(EnrollmentErrors.AlreadyActive);
        }

        world.CourseGrants.Add(request);

        return Task.FromResult<Result<CourseEnrollmentResponse>>(new CourseEnrollmentResponse
        {
            Id = 1, CustomerId = request.CustomerId!.Value, CourseId = request.CourseId, Status = EnrollmentStatus.Active,
            Source = request.Source, IsPaid = true,
        });
    }

    public Task<Result<LessonEnrollmentResponse>> GrantLessonAsync(GrantLessonAccessRequest request, long? staffId, CancellationToken cancellationToken = default)
    {
        if (world.CustomerAlreadyOwns)
        {
            return Task.FromResult<Result<LessonEnrollmentResponse>>(EnrollmentErrors.AlreadyActive);
        }

        world.LessonGrants.Add(request);

        return Task.FromResult<Result<LessonEnrollmentResponse>>(new LessonEnrollmentResponse
        {
            Id = 1, CustomerId = request.CustomerId!.Value, LessonId = request.LessonId, Status = EnrollmentStatus.Active,
            Source = request.Source, IsPaid = true,
        });
    }

    public Task<Result> RevokeCourseAsync(long enrollmentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result> RevokeLessonAsync(long enrollmentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<CustomerAccessResponse>> GetCustomerAccessAsync(long customerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<MyCourseResponse>> ListMyCoursesAsync(long customerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<MyCatalogStateResponse> GetMyCatalogStateAsync(long customerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<CourseAccessResponse>> GetStaffCourseAccessAsync(string courseSlug, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<CourseAccessResponse>> GetCourseAccessAsync(string courseSlug, long customerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<CourseQuoteResponse>> QuoteCourseAsync(long courseId, long customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Result<CourseQuoteResponse>>(new CourseQuoteResponse(world.QuotePrice, [], world.QuotePrice));
}
