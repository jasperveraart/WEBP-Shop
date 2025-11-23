using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

public partial class ProductEdit : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] private AppDbContext DbContext { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    private ProductEditModel? _model;
    private List<CategoryOption> _categoryOptions = new();
    private List<SupplierOption> _supplierOptions = new();
    private string? _statusMessage;
    private string? _errorMessage;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadOptionsAsync();
        await LoadProductAsync();
    }

    private async Task LoadOptionsAsync()
    {
        _categoryOptions = await DbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayName)
            .Select(c => new CategoryOption(c.Id, c.DisplayName))
            .ToListAsync();

        _supplierOptions = await UserManager.Users
            .AsNoTracking()
            .Where(u => u.IsSupplier)
            .OrderBy(u => u.DisplayName ?? u.CompanyName ?? u.Email ?? u.UserName)
            .Select(u => new SupplierOption(u.Id, GetSupplierDisplayName(u)))
            .ToListAsync();
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
                CategoryId = product.CategoryId,
                SupplierId = product.SupplierId,
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                LongDescription = product.LongDescription,
                QuantityAvailable = product.QuantityAvailable,
                IsFeatured = product.IsFeatured,
                IsActive = product.IsActive,
                IsListingOnly = product.IsListingOnly,
                IsSuspendedBySupplier = product.IsSuspendedBySupplier,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                BasePrice = product.BasePrice,
                MarkupPercentage = product.MarkupPercentage,
                FinalPrice = product.FinalPrice
            };

            RecalculateFinalPrice();
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

            RecalculateFinalPrice();

            product.Name = _model.Name.Trim();
            product.ShortDescription = _model.ShortDescription?.Trim() ?? string.Empty;
            product.LongDescription = _model.LongDescription?.Trim() ?? string.Empty;
            product.CategoryId = _model.CategoryId!.Value;
            product.SupplierId = _model.SupplierId!;
            product.QuantityAvailable = _model.QuantityAvailable;
            product.BasePrice = _model.BasePrice;
            product.MarkupPercentage = _model.MarkupPercentage;
            product.FinalPrice = _model.FinalPrice;
            product.IsFeatured = _model.IsFeatured;
            product.IsActive = _model.IsActive;
            product.IsListingOnly = _model.IsListingOnly;
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

    private void RecalculateFinalPrice()
    {
        if (_model is null)
        {
            return;
        }

        var basePrice = Math.Max(0, _model.BasePrice);
        var markup = Math.Max(0, _model.MarkupPercentage);
        _model.BasePrice = basePrice;
        _model.MarkupPercentage = markup;
        _model.FinalPrice = Math.Max(0, basePrice + (basePrice * markup / 100));
    }

    private Task OnBasePriceChanged(double value)
    {
        if (_model is null)
        {
            return Task.CompletedTask;
        }

        _model.BasePrice = value;
        RecalculateFinalPrice();
        return Task.CompletedTask;
    }

    private Task OnMarkupChanged(double value)
    {
        if (_model is null)
        {
            return Task.CompletedTask;
        }

        _model.MarkupPercentage = value;
        RecalculateFinalPrice();
        return Task.CompletedTask;
    }

    private Task OnCategoryChanged(int? value)
    {
        if (_model is null)
        {
            return Task.CompletedTask;
        }

        _model.CategoryId = value;
        return Task.CompletedTask;
    }

    private Task OnSupplierChanged(string? value)
    {
        if (_model is null)
        {
            return Task.CompletedTask;
        }

        _model.SupplierId = value;
        return Task.CompletedTask;
    }

    private static string GetSupplierDisplayName(ApplicationUser user)
    {
        return user.DisplayName
               ?? user.CompanyName
               ?? user.Email
               ?? user.UserName
               ?? user.Id;
    }

    private sealed class ProductEditModel
    {
        [Required]
        public int? CategoryId { get; set; }

        [Required]
        public string? SupplierId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        [StringLength(4000)]
        public string? LongDescription { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater.")]
        public int QuantityAvailable { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Base price must be zero or greater.")]
        public double BasePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Markup must be zero or greater.")]
        public double MarkupPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Final price must be zero or greater.")]
        public double FinalPrice { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        public bool IsListingOnly { get; set; }

        public bool IsSuspendedBySupplier { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    private sealed record CategoryOption(int Id, string DisplayName);

    private sealed record SupplierOption(string Id, string DisplayName);
}
