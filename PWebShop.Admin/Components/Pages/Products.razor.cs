using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

public partial class Products : ComponentBase
{
    [Inject] private AppDbContext DbContext { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    private readonly List<ProductListItem> _allProducts = new();
    private List<ProductListItem> _filteredProducts = new();
    private List<CategoryOption> _categoryOptions = new();
    private List<SupplierOption> _supplierOptions = new();

    private string? _searchTerm;
    private int? _selectedCategoryId;
    private string? _selectedSupplierId;
    private string? _statusMessage;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadFilterOptionsAsync();
            await LoadProductsAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load products. {ex.Message}";
        }
    }

    private async Task LoadFilterOptionsAsync()
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

    private async Task LoadProductsAsync()
    {
        var products = await DbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToListAsync();

        var supplierIds = products.Select(p => p.SupplierId).Distinct().ToList();
        var supplierLookup = await UserManager.Users
            .AsNoTracking()
            .Where(u => supplierIds.Contains(u.Id) || u.IsSupplier)
            .ToDictionaryAsync(u => u.Id, GetSupplierDisplayName);

        _allProducts.Clear();
        foreach (var product in products)
        {
            supplierLookup.TryGetValue(product.SupplierId, out var supplierDisplayName);
            _allProducts.Add(new ProductListItem
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.DisplayName ?? product.Category?.Name ?? "Uncategorized",
                SupplierId = product.SupplierId,
                SupplierName = supplierDisplayName ?? product.SupplierId,
                IsActive = product.IsActive,
                IsListingOnly = product.IsListingOnly,
                IsSuspendedBySupplier = product.IsSuspendedBySupplier
            });
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<ProductListItem> query = _allProducts;

        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            var term = _searchTerm.Trim();
            query = query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                     || p.Id.ToString(CultureInfo.InvariantCulture).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedCategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == _selectedCategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(_selectedSupplierId))
        {
            query = query.Where(p => p.SupplierId == _selectedSupplierId);
        }

        _filteredProducts = query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToList();
    }

    private (string Text, string CssClass) GetStatus(ProductListItem product)
    {
        if (product.IsSuspendedBySupplier)
        {
            return ("Suspended", "bg-warning text-dark");
        }

        if (product.IsActive)
        {
            return ("Active", "bg-success");
        }

        return ("Inactive", "bg-secondary");
    }

    private void OnEditProduct(ProductListItem product)
    {
        _errorMessage = null;
        _statusMessage = $"Edit product action selected for {product.Name} (ID: {product.Id}).";
    }

    private static string GetSupplierDisplayName(ApplicationUser supplier)
    {
        return supplier.CompanyName
               ?? supplier.DisplayName
               ?? supplier.Email
               ?? supplier.UserName
               ?? supplier.Id;
    }

    private sealed record CategoryOption(int Id, string DisplayName);

    private sealed record SupplierOption(string Id, string DisplayName);

    private sealed class ProductListItem
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int CategoryId { get; init; }

        public string CategoryName { get; init; } = string.Empty;

        public string SupplierId { get; init; } = string.Empty;

        public string SupplierName { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public bool IsListingOnly { get; init; }

        public bool IsSuspendedBySupplier { get; init; }
    }
}
