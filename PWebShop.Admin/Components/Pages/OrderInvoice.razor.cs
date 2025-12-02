using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

public partial class OrderInvoice : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;

    private Order? _order;
    private ApplicationUser? _customer;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrderAsync();
    }

    private async Task LoadOrderAsync()
    {
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();

            _order = await dbContext.Orders
                .AsNoTracking()
                .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
                .FirstOrDefaultAsync(o => o.Id == Id);

            if (_order != null)
            {
                _customer = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == _order.CustomerId);
            }
        }
        catch (Exception)
        {
            // Handle error silently or log it
        }
        finally
        {
            _isLoading = false;
        }
    }
}
