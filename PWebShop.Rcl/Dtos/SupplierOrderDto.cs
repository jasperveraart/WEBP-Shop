using PWebShop.Domain.Entities;

namespace PWebShop.Rcl.Dtos;

public class SupplierOrderDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public string CustomerName { get; set; } = string.Empty; // Optional, maybe just ID or "Customer"
    public decimal TotalAmount { get; set; } // Total for this supplier's items
    public List<SupplierOrderLineDto> Lines { get; set; } = new();
}
