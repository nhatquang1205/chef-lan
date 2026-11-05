using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Payments;

namespace ChefLan.Logic.Courses.Mapping;

public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order, IPaymentSettings settings, DateTime utcNow)
    {
        var status = order.EffectiveStatus(utcNow);

        return new OrderResponse
        {
            Code = order.Code,
            Status = status,
            AmountVnd = order.AmountVnd,
            CreatedAt = order.CreatedAt,
            ExpiresAt = order.ExpiresAt,
            PaidAt = order.PaidAt,
            Items = order.Items.Select(ToResponse).ToList(),
            Payment = status == OrderStatus.Pending ? PaymentInstructions.For(settings, order.Code, order.AmountVnd) : null,
        };
    }

    public static AdminOrderResponse ToAdminResponse(this Order order, DateTime utcNow) => new()
    {
        Id = order.Id,
        Code = order.Code,
        Status = order.EffectiveStatus(utcNow),
        AmountVnd = order.AmountVnd,
        CustomerId = order.CustomerId,
        CustomerName = order.Customer?.FullName,
        CustomerEmail = order.Customer?.Email,
        CreatedAt = order.CreatedAt,
        ExpiresAt = order.ExpiresAt,
        PaidAt = order.PaidAt,
        PaidVia = order.PaidVia,
        PaidReason = order.PaidReason,
        PaidByStaffId = order.PaidByStaffId,
        PaymentTransactionId = order.PaymentTransactionId,
        Items = order.Items.Select(ToResponse).ToList(),
    };

    public static PaymentTransactionResponse ToResponse(this PaymentTransaction transaction) => new()
    {
        Id = transaction.Id,
        Provider = transaction.Provider,
        ProviderTransactionId = transaction.ProviderTransactionId,
        AmountVnd = transaction.AmountVnd,
        Content = transaction.Content,
        Code = transaction.Code,
        TransactionDate = transaction.TransactionDate,
        ReceivedAt = transaction.CreatedAt,
        OrderId = transaction.OrderId,
        OrderCode = transaction.Order?.Code,
        MatchStatus = transaction.MatchStatus,
        Note = transaction.Note,
    };

    private static OrderItemResponse ToResponse(OrderItem item) =>
        new(item.Kind, item.Title, item.CourseSlug, item.ListPriceVnd, item.DiscountVnd, item.UnitPriceVnd);
}
