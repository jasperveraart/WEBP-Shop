using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PWebShop.Infrastructure;

namespace PWebShop.Admin.Components.Pages;

public partial class ProductEdit : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] private AppDbContext DbContext { get; set; } = default!;

    private ProductEditModel? _model;
    private string? _statusMessage;
    private string? _errorMessage;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadProductAsync();
    }

    private async Task LoadProductAsync()
    {
        try
        {
            var product = await DbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (product is null)
            {
                _model = null;
                _errorMessage = "Product not found.";
                return;
            }

            _model = new ProductEditModel
            {
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                QuantityAvailable = product.QuantityAvailable
            };
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load product. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (_model is null)
        {
            return;
        }

        try
        {
            var product = await DbContext.Products.FirstOrDefaultAsync(p => p.Id == Id);
            if (product is null)
            {
                _errorMessage = "Product not found.";
                return;
            }

            product.Name = _model.Name.Trim();
            product.ShortDescription = _model.ShortDescription?.Trim() ?? string.Empty;
            product.QuantityAvailable = _model.QuantityAvailable;
            product.UpdatedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();

            _statusMessage = "Product saved successfully.";
            _errorMessage = null;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save product. {ex.Message}";
            _statusMessage = null;
        }
    }

    private sealed class ProductEditModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater.")]
        public int QuantityAvailable { get; set; }
    }
}
