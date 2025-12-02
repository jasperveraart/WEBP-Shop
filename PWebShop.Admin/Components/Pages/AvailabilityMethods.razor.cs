using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;

namespace PWebShop.Admin.Components.Pages;

public partial class AvailabilityMethods : ComponentBase
{
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;

    private List<AvailabilityMethod> _methods = new();
    private bool _isLoading = true;
    private string? _statusMessage;
    private string? _errorMessage;
    private bool _isDeleteModalOpen;
    private AvailabilityMethod? _methodToDelete;

    protected override async Task OnInitializedAsync()
    {
        await LoadMethodsAsync();
    }

    private async Task LoadMethodsAsync()
    {
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            _methods = await dbContext.AvailabilityMethods
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load availability methods. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ConfirmDelete(AvailabilityMethod method)
    {
        _methodToDelete = method;
        _isDeleteModalOpen = true;
    }

    private async Task HandleDeleteConfirmed()
    {
        if (_methodToDelete is null) return;

        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            var method = await dbContext.AvailabilityMethods.FindAsync(_methodToDelete.Id);

            if (method is null)
            {
                _errorMessage = "Availability method not found.";
            }
            else
            {
                dbContext.AvailabilityMethods.Remove(method);
                await dbContext.SaveChangesAsync();
                _statusMessage = "Availability method deleted successfully.";
                await LoadMethodsAsync();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to delete availability method. {ex.Message}";
        }
        finally
        {
            _isDeleteModalOpen = false;
            _methodToDelete = null;
        }
    }

    private void HandleDeleteCancelled()
    {
        _isDeleteModalOpen = false;
        _methodToDelete = null;
    }
}
