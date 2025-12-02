using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

public partial class OrderDetails
{
    [Parameter] public int Id { get; set; }

    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private Order? _order;
    private ApplicationUser? _customer;
    private bool _isLoading = true;
    private string? _statusMessage;
    private string? _errorMessage;

    private OrderStatus _selectedStatus;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrderAsync();
    }

    private async Task LoadOrderAsync()
    {
        _isLoading = true;
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            
            _order = await dbContext.Orders
                .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
                .FirstOrDefaultAsync(o => o.Id == Id);

            if (_order != null)
            {
                _customer = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == _order.CustomerId);
                _selectedStatus = _order.Status;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading order: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OnStatusChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<OrderStatus>(e.Value?.ToString(), out var newStatus))
        {
            _selectedStatus = newStatus;
        }
    }

    private async Task SaveStatusAsync()
    {
        if (_order == null) return;
        
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            var orderToUpdate = await dbContext.Orders.FindAsync(_order.Id);
            
            if (orderToUpdate != null)
            {
                orderToUpdate.Status = _selectedStatus;
                await dbContext.SaveChangesAsync();
                
                _order.Status = _selectedStatus;
                _statusMessage = $"Order status updated to {_selectedStatus}.";
                _errorMessage = null;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to update status: {ex.Message}";
            _statusMessage = null;
        }
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

    private void GoBack()
    {
        NavigationManager.NavigateTo("/orders");
    }
}
