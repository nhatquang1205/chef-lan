using System.Security.Cryptography;
using System.Text;
using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;
using ChefLan.Logic.Courses.Payments;

namespace ChefLan.Logic.Courses.Services;

internal sealed class PaymentService(
    IOrderRepository orders,
    IPaymentTransactionRepository transactions,
    IPaymentSettings settings,
    OrderFulfilment fulfilment,
    IUnitOfWork unitOfWork,
    IClock clock) : IPaymentService
{
    public async Task<Result<PaymentIngestResponse>> IngestSePayAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        var parsed = SePayIpnParser.Parse(rawBody);

        // An unreadable body still gets a row (keyed by its hash), so staff can see it and nothing is lost.
        var providerId = parsed?.TransactionId ?? $"unreadable-{Hash(rawBody)}";
        var transaction = await transactions.GetByProviderIdAsync(settings.Provider, providerId, cancellationToken);

        if (transaction is null)
        {
            transaction = new PaymentTransaction
            {
                Provider = settings.Provider,
                ProviderTransactionId = providerId,
                AmountVnd = parsed?.Amount ?? 0,
                Content = Truncate(parsed?.Description, 1000),
                TransactionDate = parsed?.TransactionDate,
                RawPayload = rawBody,
                Note = parsed is null ? "The notification could not be read." : null,
            };

            transactions.Add(transaction);

            try
            {
                // Stored before anything else, so a failure while matching never loses the notification.
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConflictException)
            {
                // The same notification arrived twice at once and the other one won: it is stored, nothing more to do.
                var winner = await transactions.GetByProviderIdAsync(settings.Provider, providerId, cancellationToken);

                return winner is null ? OrderErrors.TransactionNotFound(0) : await OutcomeAsync(winner, cancellationToken);
            }
        }
        else if (transaction.MatchStatus != PaymentMatchStatus.Unmatched || transaction.OrderId is not null)
        {
            // A retry of a notification that was already dealt with.
            return await OutcomeAsync(transaction, cancellationToken);
        }

        if (parsed is null)
        {
            return await OutcomeAsync(transaction, cancellationToken);
        }

        if (!parsed.IsPayment || parsed.Amount <= 0)
        {
            // A void or a payment that is not captured: not money to apply. Staff can see it in the full list.
            transaction.MatchStatus = PaymentMatchStatus.Ignored;
            transaction.Note = $"{parsed.NotificationType}: not applied.";
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await OutcomeAsync(transaction, cancellationToken);
        }

        await MatchAsync(transaction, parsed, cancellationToken);

        return await OutcomeAsync(transaction, cancellationToken);
    }

    public async Task<PagedResult<PaymentTransactionResponse>> ListAsync(
        IReadOnlyCollection<PaymentMatchStatus>? statuses,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var page = await transactions.ListAsync(statuses, paging, cancellationToken);

        return PagedResult<PaymentTransactionResponse>.Create(
            page.Items.Select(t => t.ToResponse()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<Result<PaymentTransactionResponse>> LinkAsync(
        long transactionId,
        long orderId,
        long? staffId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactions.GetByIdAsync(transactionId, cancellationToken);

        if (transaction is null)
        {
            return OrderErrors.TransactionNotFound(transactionId);
        }

        // Matched and Overpaid transactions already paid their order; Ignored ones are not payments.
        if (transaction.MatchStatus is PaymentMatchStatus.Matched or PaymentMatchStatus.Overpaid or PaymentMatchStatus.Ignored)
        {
            return OrderErrors.TransactionSettled;
        }

        var order = await orders.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            return OrderErrors.NotFound(orderId);
        }

        var reason = $"Linked bank transaction {transaction.ProviderTransactionId}";

        if (order.Status == OrderStatus.Paid)
        {
            // Already paid by something else: this is a second payment, recorded against the order.
            transaction.OrderId = order.Id;
            transaction.MatchStatus = PaymentMatchStatus.Duplicate;
            transaction.Note = "Linked by staff to an order that was already paid: refund.";
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return transaction.ToResponse();
        }

        var result = await fulfilment.MarkPaidAsync(order, OrderPaidVia.Manual, transaction, staffId, reason, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        OrderService.Reconcile(transaction, order, result.Value, "Linked by staff.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.ToResponse();
    }

    private async Task MatchAsync(PaymentTransaction transaction, SePayNotification parsed, CancellationToken cancellationToken)
    {
        Order? order = null;
        string? code = null;

        foreach (var candidate in OrderCodes.FindCandidates(parsed.InvoiceNumber, parsed.Description, settings.CodePrefix))
        {
            code ??= candidate;
            order = await orders.GetByCodeAsync(candidate, cancellationToken);

            if (order is not null)
            {
                code = candidate;
                break;
            }
        }

        var decision = PaymentMatcher.Decide(order, parsed.Amount, clock.UtcNow);

        transaction.Code = code;
        transaction.OrderId = order?.Id;
        transaction.MatchStatus = decision.Status;
        transaction.Note = Describe(decision.Status);

        if (decision is { PayOrder: true, Status: var status } && order is not null)
        {
            // The transaction is already saved, so its id is set for the order to point at.
            var result = await fulfilment.MarkPaidAsync(order, OrderPaidVia.Webhook, transaction, null, null, cancellationToken);

            if (result.IsFailure)
            {
                // Granting failed for a reason that is not "already owned": leave the order Pending and the
                // transaction flagged, for staff. The notification is stored, so nothing is lost.
                transaction.MatchStatus = PaymentMatchStatus.Unmatched;
                transaction.OrderId = order.Id;
                transaction.Note = $"Paying the order failed ({result.Error.Code}). Staff: use mark-paid.";
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return;
            }

            if (result.Value)
            {
                transaction.MatchStatus = PaymentMatchStatus.Duplicate;
                transaction.Note = "The customer already had access: refund.";
            }
            else if (status == PaymentMatchStatus.Overpaid)
            {
                transaction.Note = $"Paid {transaction.AmountVnd - order.AmountVnd:N0} more than the order: refund the difference.";
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result<PaymentIngestResponse>> OutcomeAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        string? orderCode = transaction.Code;

        if (transaction.OrderId is { } id && transaction.Order is null)
        {
            orderCode = (await orders.GetByIdAsync(id, cancellationToken))?.Code ?? orderCode;
        }

        return new PaymentIngestResponse(transaction.Id, transaction.MatchStatus, orderCode ?? transaction.Order?.Code);
    }

    private static string? Describe(PaymentMatchStatus status) => status switch
    {
        PaymentMatchStatus.Underpaid => "Less than the order total: the order stays pending.",
        PaymentMatchStatus.LateAfterExpiry => "The order had expired or was cancelled: link it to an order or refund.",
        PaymentMatchStatus.Duplicate => "The order was already paid: refund.",
        PaymentMatchStatus.Unmatched => "No matching order code (invoice number).",
        _ => null,
    };

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    private static string Hash(string body) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)))[..32].ToLowerInvariant();
}
