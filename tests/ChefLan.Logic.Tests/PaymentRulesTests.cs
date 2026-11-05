using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Payments;

namespace ChefLan.Logic.Tests;

public class OrderCodesTests
{
    [Fact]
    public void Generated_codes_have_the_prefix_and_only_safe_characters()
    {
        for (var i = 0; i < 200; i++)
        {
            var code = OrderCodes.Generate("bb");

            Assert.StartsWith("BB", code);
            Assert.Equal(2 + OrderCodes.RandomLength, code.Length);
            Assert.All(code[2..], c => Assert.Contains(c, OrderCodes.Alphabet));
        }
    }

    [Theory]
    [InlineData("BB7K2M9Q")]
    [InlineData("bb7k2m9q")]
    [InlineData("MBVCB.123456.Thanh toan BB7K2M9Q FT2631")]
    [InlineData("NGUYEN VAN A chuyen khoan BB 7K2M 9Q")]
    [InlineData("BB-7K2M-9Q")]
    public void A_code_is_found_however_the_bank_mangles_the_note(string content)
    {
        Assert.Contains("BB7K2M9Q", OrderCodes.FindCandidates(null, content, "BB"));
    }

    [Fact]
    public void The_provider_code_field_comes_first_when_it_fits()
    {
        var found = OrderCodes.FindCandidates("BB7K2M9Q", "note BBABCDEF", "BB");

        Assert.Equal("BB7K2M9Q", found[0]);
        Assert.Contains("BBABCDEF", found);
    }

    [Fact]
    public void A_name_containing_the_prefix_does_not_hide_the_real_code()
    {
        // "TRAN NGOC BBBBB" squashed puts BB ahead of the real code; every candidate is offered so the caller can try each.
        var found = OrderCodes.FindCandidates(null, "TRAN BBB BB7K2M9Q", "BB");

        Assert.Contains("BB7K2M9Q", found);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("chuyen tien an trua")]
    [InlineData("BB0O1I")]
    public void A_note_without_a_code_gives_nothing(string? content)
    {
        Assert.Empty(OrderCodes.FindCandidates(null, content, "BB"));
    }
}

public class PaymentMatcherTests
{
    private static readonly DateTime Now = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private static Order OrderOf(decimal amount, OrderStatus status = OrderStatus.Pending, int expiresInMinutes = 60) => new()
    {
        Code = "BB7K2M9Q",
        AmountVnd = amount,
        Status = status,
        ExpiresAt = Now.AddMinutes(expiresInMinutes),
    };

    [Fact]
    public void Exact_amount_pays_the_order()
    {
        var decision = PaymentMatcher.Decide(OrderOf(1_000_000), 1_000_000, Now);

        Assert.Equal(new MatchDecision(PaymentMatchStatus.Matched, true), decision);
    }

    [Fact]
    public void More_than_the_total_still_pays_and_is_flagged_for_a_refund()
    {
        Assert.Equal(new MatchDecision(PaymentMatchStatus.Overpaid, true), PaymentMatcher.Decide(OrderOf(1_000_000), 1_200_000, Now));
    }

    [Fact]
    public void Less_than_the_total_leaves_the_order_unpaid()
    {
        Assert.Equal(new MatchDecision(PaymentMatchStatus.Underpaid, false), PaymentMatcher.Decide(OrderOf(1_000_000), 999_999, Now));
    }

    [Fact]
    public void A_second_transfer_to_a_paid_order_is_a_duplicate()
    {
        Assert.Equal(new MatchDecision(PaymentMatchStatus.Duplicate, false), PaymentMatcher.Decide(OrderOf(1_000_000, OrderStatus.Paid), 1_000_000, Now));
    }

    [Theory]
    [InlineData(OrderStatus.Cancelled, 60)]
    [InlineData(OrderStatus.Expired, 60)]
    [InlineData(OrderStatus.Pending, -1)]
    public void A_late_payment_is_never_granted_automatically(OrderStatus status, int expiresInMinutes)
    {
        Assert.Equal(
            new MatchDecision(PaymentMatchStatus.LateAfterExpiry, false),
            PaymentMatcher.Decide(OrderOf(1_000_000, status, expiresInMinutes), 1_000_000, Now));
    }

    [Fact]
    public void An_unknown_order_is_unmatched()
    {
        Assert.Equal(new MatchDecision(PaymentMatchStatus.Unmatched, false), PaymentMatcher.Decide(null, 1_000_000, Now));
    }
}

public class SePayIpnParserTests
{
    // The example from SePay's IPN page, with the invoice number our code.
    private const string Typical = """
        {"timestamp": 1757000000, "notification_type": "ORDER_PAID",
         "order": {"id": "e2c195be", "order_id": "NPSETVI00101000042R", "order_status": "CAPTURED", "order_currency": "VND",
                   "order_amount": "50000.00", "order_invoice_number": "BB7K2M9Q", "custom_data": [],
                   "order_description": "Bon Bocon BB7K2M9Q"},
         "transaction": {"id": "384c66dd", "payment_method": "BANK_TRANSFER", "transaction_id": "68ba94ac80123",
                         "transaction_type": "PAYMENT", "transaction_date": "2025-09-01 19:00:15", "transaction_status": "APPROVED",
                         "transaction_amount": "50000", "transaction_currency": "VND"},
         "customer": {"id": "bae12d2f", "customer_id": "CUST_001"}}
        """;

    [Fact]
    public void Reads_a_typical_notification()
    {
        var n = SePayIpnParser.Parse(Typical)!;

        Assert.Equal("68ba94ac80123", n.TransactionId);
        Assert.Equal(50_000m, n.Amount);
        Assert.True(n.IsPayment);
        Assert.Equal("ORDER_PAID", n.NotificationType);
        Assert.Equal("BB7K2M9Q", n.InvoiceNumber);
        Assert.Equal("Bon Bocon BB7K2M9Q", n.Description);
        // Local time (UTC+7) becomes UTC.
        Assert.Equal(new DateTime(2025, 9, 1, 12, 0, 15, DateTimeKind.Utc), n.TransactionDate);
    }

    [Fact]
    public void The_amount_comes_from_the_transaction_and_falls_back_to_the_order()
    {
        var body = """{"notification_type":"ORDER_PAID","order":{"order_id":"X","order_amount":"70000.00","order_invoice_number":"BBAAAAAA"},"transaction":{"transaction_id":"T1"}}""";

        Assert.Equal(70_000m, SePayIpnParser.Parse(body)!.Amount);
    }

    [Theory]
    [InlineData("TRANSACTION_VOID", "APPROVED", "CAPTURED")]
    [InlineData("ORDER_PAID", "DECLINED", "CAPTURED")]
    [InlineData("ORDER_PAID", "APPROVED", "AUTHORIZED")]
    public void Only_an_approved_captured_payment_counts_as_money(string type, string transactionStatus, string orderStatus)
    {
        var body = """{"notification_type":"@T@","order":{"order_status":"@O@","order_amount":"1000","order_invoice_number":"BB7K2M9Q"},"transaction":{"transaction_id":"T1","transaction_status":"@S@","transaction_amount":1000}}"""
            .Replace("@T@", type).Replace("@S@", transactionStatus).Replace("@O@", orderStatus);

        Assert.False(SePayIpnParser.Parse(body)!.IsPayment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("""{"notification_type":"ORDER_PAID"}""")]
    [InlineData("""{"order":{"order_amount":"5"},"transaction":{"transaction_id":"1"}}""")]
    [InlineData("""{"notification_type":"ORDER_PAID","order":{"order_invoice_number":"BB7K2M9Q"}}""")]
    public void An_unreadable_body_gives_null_rather_than_a_guess(string? body)
    {
        Assert.Null(SePayIpnParser.Parse(body));
    }
}

public class PaymentInstructionsTests
{
    [Fact]
    public void The_form_is_signed_with_the_documented_algorithm()
    {
        var form = PaymentInstructions.For(new FakeSettings(), "BB7K2M9Q", 1_000_000);

        Assert.Equal("https://pay.example/v1/checkout/init", form.ActionUrl);
        Assert.Equal(1_000_000, form.AmountVnd);

        string Field(string name) => form.Fields.Single(f => f.Name == name).Value;

        Assert.Equal("SP-TEST-1", Field("merchant"));
        Assert.Equal("1000000", Field("order_amount"));
        Assert.Equal("BB7K2M9Q", Field("order_invoice_number"));
        Assert.Equal("BANK_TRANSFER", Field("payment_method"));
        Assert.Equal("https://shop.example/checkout/BB7K2M9Q?result=success", Field("success_url"));
        // HMAC-SHA256 (Base64) of the comma-joined name=value pairs in SePay's order, key "test-secret": computed outside .NET.
        Assert.Equal("hsiKxIcfNHXXzc7EfyTsqjrEWO5kUMwVILT/jifUH14=", Field("signature"));
        Assert.Equal("signature", form.Fields[^1].Name);
    }

    [Fact]
    public void Empty_fields_stay_out_of_the_signature_and_the_secret_never_appears_in_the_form()
    {
        var form = PaymentInstructions.For(new FakeSettings(), "BB7K2M9Q", 1_000_000);

        Assert.DoesNotContain(form.Fields, f => f.Value.Contains("test-secret"));
        Assert.DoesNotContain(form.Fields, f => f.Name == "customer_id");

        var withExtra = PaymentInstructions.Sign(
            [new("order_amount", "1000"), new("merchant", "M"), new("customer_id", ""), new("lang", "vi")],
            "k");
        var without = PaymentInstructions.Sign([new("order_amount", "1000"), new("merchant", "M")], "k");

        // An empty value and an unsigned field (lang) change nothing.
        Assert.Equal(without, withExtra);
    }

    [Fact]
    public void A_different_amount_gives_a_different_signature()
    {
        string Signature(decimal amount) => PaymentInstructions.For(new FakeSettings(), "BB7K2M9Q", amount).Fields[^1].Value;

        Assert.NotEqual(Signature(1_000_000), Signature(999_999));
    }
}
