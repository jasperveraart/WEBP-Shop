using PWebShop.Rcl.Dtos;

namespace PWebShop.Rcl.Services;

public interface IOrderService
{
    Task<OrderDto?> CreateOrder(OrderCreateDto dto);
    Task<OrderDto?> GetOrderById(int id);
    Task<IEnumerable<OrderDto>> GetOrders();
    Task<bool> SimulatePayment(int orderId, string paymentMethod);
    Task<bool> CancelOrder(int orderId);
}
