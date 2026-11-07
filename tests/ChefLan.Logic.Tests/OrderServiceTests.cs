using System.Reflection;
using ChefLan.Commons.Results;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Payments;
using ChefLan.Logic.Courses.Services;

namespace ChefLan.Logic.Tests;

public class OrderServiceTests
{
    private const long CustomerId = 5;

    private static CreateOrderRequest Contact => new("macaron", Method: OrderPaymentMethod.ContactChef);

    private static CreateOrderRequest Transfer => new("macaron", Method: OrderPaymentMethod.BankTransfer);

    [Fact]
    public async Task A_contact_order_is_created_while_checkout_is_off()
    {
        var world = new FakeWorld();
        world.Settings.CheckoutEnabled = false;

        var result = await new OrderHarness(world).Service.CreateAsync(CustomerId, Contact);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderPaymentMethod.ContactChef, result.Value.PaymentMethod);
        Assert.Equal(OrderStatus.Pending, result.Value.Status);
        Assert.Single(world.Orders);
    }

    [Fact]
    public async Task A_transfer_order_is_still_refused_while_checkout_is_off()
    {
        var world = new FakeWorld();
        world.Settings.CheckoutEnabled = false;

        var result = await new OrderHarness(world).Service.CreateAsync(CustomerId, Transfer);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CheckoutDisabled, result.Error);
        Assert.Empty(world.Orders);
    }

    [Fact]
    public async Task A_contact_order_has_no_payment_form_and_lives_seven_days()
    {
        var world = new FakeWorld();

        var result = await new OrderHarness(world).Service.CreateAsync(CustomerId, Contact);

        Assert.Null(result.Value.Payment);
        Assert.Equal(world.Now.AddDays(7), result.Value.ExpiresAt);
    }

    [Fact]
    public async Task A_transfer_order_has_a_payment_form_and_lives_a_day()
    {
        var world = new FakeWorld();

        var result = await new OrderHarness(world).Service.CreateAsync(CustomerId, Transfer);

        Assert.NotNull(result.Value.Payment);
        Assert.Equal(world.Now.AddHours(24), result.Value.ExpiresAt);
    }

    [Fact]
    public async Task Changing_the_method_keeps_the_order_and_its_code()
    {
        var world = new FakeWorld();
        var service = new OrderHarness(world).Service;

        var first = await service.CreateAsync(CustomerId, Transfer);
        var second = await service.CreateAsync(CustomerId, Contact);

        Assert.Equal(first.Value.Code, second.Value.Code);
        Assert.Single(world.Orders);
        Assert.Equal(OrderPaymentMethod.ContactChef, second.Value.PaymentMethod);
        Assert.Null(second.Value.Payment);
        Assert.Equal(world.Now.AddDays(7), second.Value.ExpiresAt);

        var back = await service.CreateAsync(CustomerId, Transfer);

        Assert.Equal(first.Value.Code, back.Value.Code);
        Assert.NotNull(back.Value.Payment);
        Assert.Equal(world.Now.AddHours(24), back.Value.ExpiresAt);
    }

    [Fact]
    public async Task Staff_approving_a_contact_order_grants_access()
    {
        var world = new FakeWorld();
        var service = new OrderHarness(world).Service;
        var created = await service.CreateAsync(CustomerId, Contact);
        var order = world.Orders.Single();

        var result = await service.MarkPaidAsync(order.Id, new MarkOrderPaidRequest("Paid chef Lan in cash"), staffId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Code, result.Value.Code);
        Assert.Equal(OrderStatus.Paid, result.Value.Status);
        Assert.Equal(OrderPaymentMethod.ContactChef, result.Value.PaymentMethod);
        Assert.Equal(OrderPaidVia.Manual, order.PaidVia);
        Assert.Single(world.CourseGrants);
    }

    [Fact]
    public async Task Staff_can_approve_a_contact_order_after_it_expired()
    {
        var world = new FakeWorld();
        var service = new OrderHarness(world).Service;
        await service.CreateAsync(CustomerId, Contact);
        var order = world.Orders.Single();
        world.Now = world.Now.AddDays(8);

        Assert.Equal(OrderStatus.Expired, order.EffectiveStatus(world.Now));

        var result = await service.MarkPaidAsync(order.Id, new MarkOrderPaidRequest("Paid late"), staffId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Single(world.CourseGrants);
    }

    [Fact]
    public async Task A_multi_item_order_records_each_items_own_amount_on_its_grant()
    {
        var world = new FakeWorld();
        var service = new OrderHarness(world).Service;
        var order = world.AddPendingOrder(amount: 1_300_000);
        order.Items = [Item(10, 800_000, 742_857), Item(11, 750_000, 557_143)];

        var result = await service.MarkPaidAsync(order.Id, new MarkOrderPaidRequest("Staff order"), staffId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal([742_857m, 557_143m], world.CourseGrants.Select(g => g.AmountPaidVnd!.Value));
        Assert.Equal([57_143m, 192_857m], world.CourseGrants.Select(g => g.DiscountVnd!.Value));
    }

    [Fact]
    public async Task A_single_item_order_still_records_the_amount_received()
    {
        var world = new FakeWorld();
        var service = new OrderHarness(world).Service;
        var order = world.AddPendingOrder(amount: 900_000);

        await service.MarkPaidAsync(order.Id, new MarkOrderPaidRequest("Paid"), staffId: 1);

        Assert.Equal(900_000m, world.CourseGrants.Single().AmountPaidVnd);
    }

    private static OrderItem Item(long courseId, decimal list, decimal unit) => new()
    {
        Kind = OrderItemKind.Course,
        CourseId = courseId,
        Title = $"Course {courseId}",
        CourseSlug = $"course-{courseId}",
        ListPriceVnd = list,
        DiscountVnd = list - unit,
        UnitPriceVnd = unit,
    };

    [Fact]
    public async Task A_second_approval_is_already_paid()
    {
        var world = new FakeWorld();
        var service = new OrderHarness(world).Service;
        await service.CreateAsync(CustomerId, Contact);
        var order = world.Orders.Single();

        await service.MarkPaidAsync(order.Id, new MarkOrderPaidRequest("Paid"), staffId: 1);
        var again = await service.MarkPaidAsync(order.Id, new MarkOrderPaidRequest("Paid"), staffId: 1);

        Assert.True(again.IsFailure);
        Assert.Equal(OrderErrors.AlreadyPaid, again.Error);
        Assert.Single(world.CourseGrants);
    }
}

internal sealed class OrderHarness
{
    public OrderHarness(FakeWorld world)
    {
        var clock = new FakeClock(world);
        var unitOfWork = new FakeUnitOfWork(world);
        var enrollmentService = new FakeEnrollmentService(world);

        var courses = Stub.Of<ICourseRepository>(new()
        {
            [nameof(ICourseRepository.GetPublishedIdBySlugAsync)] = _ => Task.FromResult<long?>(10),
            [nameof(ICourseRepository.GetByIdAsync)] = _ => Task.FromResult<Course?>(new Course { Id = 10, Slug = "macaron", Title = "Macaron" }),
        });

        var enrollments = Stub.Of<IEnrollmentRepository>(new()
        {
            [nameof(IEnrollmentRepository.OwnedCourseIdsAsync)] = _ => Task.FromResult<IReadOnlySet<long>>(new HashSet<long>()),
        });

        var customers = Stub.Of<ICustomerDirectory>(new()
        {
            [nameof(ICustomerDirectory.FindAsync)] = _ => Task.FromResult<CustomerRef?>(new CustomerRef(5, "a@b.c", "An", true)),
        });

        Service = new OrderService(
            new FakeOrderRepository(world),
            new FakeTransactionRepository(world),
            courses,
            Stub.Of<ILessonRepository>(new()),
            enrollments,
            enrollmentService,
            Stub.Of<ICourseIncludeRepository>(new()),
            customers,
            world.Settings,
            new OrderFulfilment(enrollmentService, unitOfWork, clock),
            unitOfWork,
            clock);
    }

    public OrderService Service { get; }
}

/// <summary>An interface stand-in that answers only the methods it was given and throws on any other call.</summary>
internal class Stub : DispatchProxy
{
    private Dictionary<string, Func<object?[], object?>> _handlers = [];

    public static T Of<T>(Dictionary<string, Func<object?[], object?>> handlers)
        where T : class
    {
        var proxy = Create<T, Stub>();
        ((Stub)(object)proxy)._handlers = handlers;

        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        _handlers.TryGetValue(targetMethod!.Name, out var handler)
            ? handler(args ?? [])
            : throw new NotSupportedException(targetMethod.Name);
}
