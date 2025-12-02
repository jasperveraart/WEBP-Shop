using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;

namespace PWebShop.Admin.Components.Pages;

public partial class AvailabilityMethodEdit : ComponentBase
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private AvailabilityMethodModel _model = new();
    private bool _isLoading = true;
    private string? _errorMessage;

    private bool IsEditMode => Id.HasValue;

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            await LoadMethodAsync();
        }
        else
        {
            _model = new AvailabilityMethodModel { IsActive = true };
            _isLoading = false;
        }
    }

    private async Task LoadMethodAsync()
    {
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            var method = await dbContext.AvailabilityMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == Id);

            if (method is null)
            {
                _errorMessage = "Availability method not found.";
                return;
            }

            _model = new AvailabilityMethodModel
            {
                Name = method.Name,
                DisplayName = method.DisplayName,
                Description = method.Description,
                IsActive = method.IsActive
            };
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load availability method. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();

            if (IsEditMode)
            {
                var method = await dbContext.AvailabilityMethods.FindAsync(Id);
                if (method is null)
                {
                    _errorMessage = "Availability method not found.";
                    return;
                }

                method.Name = _model.Name;
                method.DisplayName = _model.DisplayName;
                method.Description = _model.Description;
                method.IsActive = _model.IsActive;
            }
            else
            {
                var method = new AvailabilityMethod
                {
                    Name = _model.Name,
                    DisplayName = _model.DisplayName,
                    Description = _model.Description,
                    IsActive = _model.IsActive
                };
                dbContext.AvailabilityMethods.Add(method);
            }

            await dbContext.SaveChangesAsync();
            NavigationManager.NavigateTo("/availability-methods");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save availability method. {ex.Message}";
        }
    }

    private class AvailabilityMethodModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
