using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Payments;
using ChefLan.Logic.Courses.Services;

namespace ChefLan.Logic.Tests;

public class PaymentServiceTests
{
    /// <summary>A SePay Payment Gateway notification as the IPN page documents it. The order code travels as the invoice number.</summary>
    private static string Body(string id, decimal amount, string content, string type = "ORDER_PAID", string? invoice = null, string transactionStatus = "APPROVED")
    {
        var invoiceNumber = invoice ?? OrderCodes.FindCandidates(null, content, "BB").FirstOrDefault() ?? "";

        return """
            {"timestamp": 1762300000, "notification_type": "@TYPE@",
             "order": {"id": "abc", "order_id": "NP-1", "order_status": "CAPTURED", "order_currency": "VND", "order_amount": "@AMOUNT@.00",
                       "order_invoice_number": "@INVOICE@", "order_description": "@CONTENT@"},
             "transaction": {"id": "t-@ID@", "payment_method": "BANK_TRANSFER", "transaction_id": "@ID@", "transaction_type": "PAYMENT",
                             "transaction_date": "2026-11-05 19:00:00", "transaction_status": "@STATUS@", "transaction_amount": @AMOUNT@},
             "customer": {"id": "c", "customer_id": "5"}}
            """
            .Replace("@TYPE@", type).Replace("@AMOUNT@", amount.ToString("0")).Replace("@INVOICE@", invoiceNumber)
            .Replace("@CONTENT@", content).Replace("@ID@", id).Replace("@STATUS@", transactionStatus);
    }

    [Fact]
    public async Task An_exact_transfer_pays_the_order_and_grants_access_once()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder(1_000_000);
        var payments = world.Payments();

        var result = await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "Chuyen tien BB7K2M9Q"));

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentMatchStatus.Matched, result.Value.MatchStatus);
        Assert.Equal("BB7K2M9Q", result.Value.OrderCode);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(OrderPaidVia.Webhook, order.PaidVia);
        Assert.Equal(world.Now, order.PaidAt);

        var grant = Assert.Single(world.CourseGrants);
        Assert.Equal(10, grant.CourseId);
        Assert.Equal(5, grant.CustomerId);
        Assert.Equal(EnrollmentSource.OnlineCheckout, grant.Source);
        Assert.True(grant.IsPaid);
        Assert.Equal(1_000_000, grant.AmountPaidVnd);
        Assert.Equal(1_100_000, grant.ListPriceVnd);
        Assert.Equal(100_000, grant.DiscountVnd);

        var transaction = Assert.Single(world.Transactions);
        Assert.Equal(order.Id, transaction.OrderId);
        Assert.Equal(transaction.Id, order.PaymentTransactionId);
    }

    [Fact]
    public async Task The_same_notification_twice_changes_nothing_the_second_time()
    {
        var world = new FakeWorld();
        world.AddPendingOrder();
        var payments = world.Payments();

        await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));
        var again = await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));

        Assert.True(again.IsSuccess);
        Assert.Equal(PaymentMatchStatus.Matched, again.Value.MatchStatus);
        Assert.Single(world.CourseGrants);
        Assert.Single(world.Transactions);
    }

    [Fact]
    public async Task A_lesson_order_grants_the_lesson()
    {
        var world = new FakeWorld();
        world.AddPendingOrder(300_000, lesson: true);

        await world.Payments().Service.IngestSePayAsync(Body("1", 300_000, "BB7K2M9Q"));

        Assert.Empty(world.CourseGrants);
        var grant = Assert.Single(world.LessonGrants);
        Assert.Equal(20, grant.LessonId);
        Assert.Equal(EnrollmentSource.OnlineCheckout, grant.Source);
    }

    [Fact]
    public async Task A_short_transfer_leaves_the_order_pending()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder(1_000_000);

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 900_000, "BB7K2M9Q"));

        Assert.Equal(PaymentMatchStatus.Underpaid, result.Value.MatchStatus);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Empty(world.CourseGrants);
        Assert.Equal(order.Id, world.Transactions[0].OrderId);
    }

    [Fact]
    public async Task An_overpayment_pays_the_order_and_says_to_refund_the_difference()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder(1_000_000);

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_250_000, "BB7K2M9Q"));

        Assert.Equal(PaymentMatchStatus.Overpaid, result.Value.MatchStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Single(world.CourseGrants);
        Assert.Contains("refund", world.Transactions[0].Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_transfer_with_no_code_is_stored_unmatched()
    {
        var world = new FakeWorld();
        world.AddPendingOrder();

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_000_000, "tien an trua", invoice: ""));

        Assert.Equal(PaymentMatchStatus.Unmatched, result.Value.MatchStatus);
        Assert.Null(result.Value.OrderCode);
        Assert.Empty(world.CourseGrants);
    }

    [Fact]
    public async Task A_code_that_names_no_order_is_stored_unmatched()
    {
        var world = new FakeWorld();

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));

        Assert.Equal(PaymentMatchStatus.Unmatched, result.Value.MatchStatus);
        Assert.Equal("BB7K2M9Q", world.Transactions[0].Code);
    }

    [Fact]
    public async Task A_payment_after_the_order_expired_is_not_granted()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder(expiresInMinutes: -5);

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));

        Assert.Equal(PaymentMatchStatus.LateAfterExpiry, result.Value.MatchStatus);
        Assert.Empty(world.CourseGrants);
        Assert.NotEqual(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task A_second_transfer_to_a_paid_order_is_a_duplicate()
    {
        var world = new FakeWorld();
        world.AddPendingOrder();
        var payments = world.Payments();

        await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));
        var second = await payments.Service.IngestSePayAsync(Body("2", 1_000_000, "BB7K2M9Q"));

        Assert.Equal(PaymentMatchStatus.Duplicate, second.Value.MatchStatus);
        Assert.Single(world.CourseGrants);
    }

    [Fact]
    public async Task A_customer_who_already_owns_it_gets_a_paid_order_and_a_refund_flag()
    {
        var world = new FakeWorld { CustomerAlreadyOwns = true };
        var order = world.AddPendingOrder();

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));

        Assert.Equal(PaymentMatchStatus.Duplicate, result.Value.MatchStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Contains("refund", world.Transactions[0].Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_voided_transaction_is_ignored()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder();

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q", type: "TRANSACTION_VOID"));

        Assert.Equal(PaymentMatchStatus.Ignored, result.Value.MatchStatus);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task A_payment_that_was_not_approved_is_ignored()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder();

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q", transactionStatus: "DECLINED"));

        Assert.Equal(PaymentMatchStatus.Ignored, result.Value.MatchStatus);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Empty(world.CourseGrants);
    }

    [Fact]
    public async Task An_unreadable_body_is_kept_for_staff_and_stored_only_once()
    {
        var world = new FakeWorld();
        var payments = world.Payments();

        var first = await payments.Service.IngestSePayAsync("{ nonsense");
        var second = await payments.Service.IngestSePayAsync("{ nonsense");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var stored = Assert.Single(world.Transactions);
        Assert.Equal("{ nonsense", stored.RawPayload);
        Assert.Equal(PaymentMatchStatus.Unmatched, stored.MatchStatus);
        Assert.Contains("could not be read", stored.Note);
    }

    [Fact]
    public async Task A_retry_after_the_order_appeared_matches_it()
    {
        var world = new FakeWorld();
        var payments = world.Payments();

        // The transfer arrives before the order is visible: stored, unmatched.
        await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));
        var order = world.AddPendingOrder();

        var retry = await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));

        Assert.Equal(PaymentMatchStatus.Matched, retry.Value.MatchStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Single(world.Transactions);
    }

    [Fact]
    public async Task Two_deliveries_racing_to_store_the_same_notification_end_with_one_row()
    {
        var world = new FakeWorld { NextInsertLosesRace = true };
        world.AddPendingOrder();

        var result = await world.Payments().Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));

        Assert.True(result.IsSuccess);
        Assert.Single(world.Transactions);
    }

    [Fact]
    public async Task Staff_can_link_an_unmatched_transaction_to_an_order()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder();
        var payments = world.Payments();
        await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "chuyen khoan", invoice: ""));

        var linked = await payments.Service.LinkAsync(world.Transactions[0].Id, order.Id, staffId: 42);

        Assert.True(linked.IsSuccess);
        Assert.Equal(PaymentMatchStatus.Matched, linked.Value.MatchStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(OrderPaidVia.Manual, order.PaidVia);
        Assert.Equal(42, order.PaidByStaffId);
        Assert.Contains("1", order.PaidReason);
        Assert.Single(world.CourseGrants);
    }

    [Fact]
    public async Task Linking_to_an_order_that_is_already_paid_records_a_duplicate_and_grants_nothing_more()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder();
        var payments = world.Payments();
        await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));
        await payments.Service.IngestSePayAsync(Body("2", 1_000_000, "chuyen khoan", invoice: ""));

        var linked = await payments.Service.LinkAsync(world.Transactions[1].Id, order.Id, staffId: 42);

        Assert.Equal(PaymentMatchStatus.Duplicate, linked.Value.MatchStatus);
        Assert.Single(world.CourseGrants);
    }

    [Fact]
    public async Task A_transaction_that_already_paid_an_order_cannot_be_linked_again()
    {
        var world = new FakeWorld();
        var order = world.AddPendingOrder();
        var other = world.AddPendingOrder(code: "BBAAAAAA");
        var payments = world.Payments();
        await payments.Service.IngestSePayAsync(Body("1", 1_000_000, "BB7K2M9Q"));

        var linked = await payments.Service.LinkAsync(world.Transactions[0].Id, other.Id, staffId: 42);

        Assert.True(linked.IsFailure);
        Assert.Equal(OrderErrors.TransactionSettled.Code, linked.Error.Code);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }
}

/// <summary>The real PaymentService over the fakes.</summary>
internal sealed class PaymentHarness
{
    public PaymentHarness(FakeWorld world)
    {
        var clock = new FakeClock(world);
        var unitOfWork = new FakeUnitOfWork(world);
        var fulfilment = new OrderFulfilment(new FakeEnrollmentService(world), unitOfWork, clock);

        Service = new PaymentService(
            new FakeOrderRepository(world),
            new FakeTransactionRepository(world),
            world.Settings,
            fulfilment,
            unitOfWork,
            clock);
    }

    public IPaymentService Service { get; }
}
