using PWebShop.Domain.Entities;

namespace PWebShop.Api.Application.Orders;

public sealed class OrderCreationResult
{
    private OrderCreationResult(bool succeeded, Order? order, string? errorMessage)
    {
        Succeeded = succeeded;
        Order = order;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }

    public Order? Order { get; }

    public string? ErrorMessage { get; }

    public static OrderCreationResult Success(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return new OrderCreationResult(true, order, null);
    }

    public static OrderCreationResult Failure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new OrderCreationResult(false, null, message);
    }
}
