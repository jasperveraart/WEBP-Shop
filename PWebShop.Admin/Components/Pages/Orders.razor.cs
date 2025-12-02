using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

public partial class Orders
{
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<OrderListItem> _orders = new();
    private List<OrderListItem> _filteredOrders = new();
    private bool _isLoading = true;

    // Filters
    private string _searchTerm = "";
    private OrderStatus? _statusFilter;

    // Pagination
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalPages = 1;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        _isLoading = true;
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            
            // We need to join with Users to get customer name
            // Since CustomerId is a string (Guid) in Order, and Id is string in AspNetUsers
            var query = from o in dbContext.Orders
                        join u in dbContext.Users on o.CustomerId equals u.Id into users
                        from u in users.DefaultIfEmpty()
                        select new OrderListItem
                        {
                            Id = o.Id,
                            OrderDate = o.OrderDate,
                            CustomerName = u != null ? (u.DisplayName ?? u.UserName ?? u.Email ?? "Unknown") : "Unknown",
                            TotalAmount = o.TotalAmount,
                            Status = o.Status,
                            ItemCount = o.OrderLines.Sum(ol => ol.Quantity)
                        };

            _orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading orders: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var query = _orders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            query = query.Where(o => 
                o.Id.ToString().Contains(_searchTerm) || 
                o.CustomerName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (_statusFilter.HasValue)
        {
            query = query.Where(o => o.Status == _statusFilter.Value);
        }

        _filteredOrders = query.ToList();
        _totalPages = (int)Math.Ceiling(_filteredOrders.Count / (double)_pageSize);
        _currentPage = 1;
    }

    private IEnumerable<OrderListItem> GetPagedOrders()
    {
        return _filteredOrders
            .Skip((_currentPage - 1) * _pageSize)
            .Take(_pageSize);
    }

    private void OnSearchChanged(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? "";
        ApplyFilters();
    }

    private void OnStatusFilterChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<OrderStatus>(e.Value?.ToString(), out var status))
        {
            _statusFilter = status;
        }
        else
        {
            _statusFilter = null;
        }
        ApplyFilters();
    }

    private void ChangePage(int page)
    {
        if (page < 1 || page > _totalPages) return;
        _currentPage = page;
    }

    private void ViewOrder(int orderId)
    {
        NavigationManager.NavigateTo($"/orders/{orderId}");
    }

    private string GetStatusBadgeClass(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.PendingPayment => "bg-warning text-dark",
            OrderStatus.Paid => "bg-info text-dark",
            OrderStatus.Shipped => "bg-primary",
            OrderStatus.Completed => "bg-success",
            OrderStatus.Cancelled => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public class OrderListItem
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public int ItemCount { get; set; }
    }
}
