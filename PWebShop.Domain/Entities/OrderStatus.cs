namespace PWebShop.Domain.Entities;

public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    Cancelled = 2,
    Shipped = 3,
    Completed = 4
}
