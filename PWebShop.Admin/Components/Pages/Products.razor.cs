using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

public partial class Products : ComponentBase
{
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private readonly List<ProductListItem> _allProducts = new();
    private List<ProductListItem> _filteredProducts = new();
    private List<ProductListItem> _pagedProducts = new();
    private List<CategoryOption> _categoryOptions = new();
    private List<SupplierOption> _supplierOptions = new();
    private List<StatusOption> _statusOptions = new();
    private readonly Dictionary<int, HashSet<int>> _categoryDescendants = new();
    private readonly int[] _pageSizeOptions = [25, 50, 100];
    private int _totalPages = 1;

    private string? _searchTerm;
    private int? _selectedCategoryId;
    private string? _selectedSupplierId;
    private string? _selectedStatus;
    private string? _statusMessage;
    private string? _errorMessage;
    private int _currentPage = 1;
    private int _pageSize = 25;

    // eigenschappen die je in de razor bindt
    public string? SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (_searchTerm == value) return;
            _searchTerm = value;
            ApplyFilters();
        }
    }

    public int? SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (_selectedCategoryId == value) return;
            _selectedCategoryId = value;
            ApplyFilters();
        }
    }

    public string? SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            if (_selectedSupplierId == value) return;
            _selectedSupplierId = value;
            ApplyFilters();
        }
    }

    public string? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus == value) return;
            _selectedStatus = value;
            ApplyFilters();
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize == value) return;
            _pageSize = value;
            _currentPage = 1;
            ApplyPagination();
        }
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        SearchTerm = e.Value?.ToString();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadFilterOptionsAsync();
            await LoadProductsAsync();
            BuildStatusOptions();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load products. {ex.Message}";
        }
    }

    private async Task LoadFilterOptionsAsync()
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();

        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        BuildCategoryOptions(categories);

        _supplierOptions = await UserManager.Users
            .AsNoTracking()
            .Where(u => u.IsSupplier)
            .OrderBy(u => u.DisplayName ?? u.CompanyName ?? u.Email ?? u.UserName)
            .Select(u => new SupplierOption(u.Id, GetSupplierDisplayName(u)))
            .ToListAsync();
    }

    private async Task LoadProductsAsync()
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();

        var products = await dbContext.Products
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
                IsSuspendedBySupplier = product.IsSuspendedBySupplier,
                Status = product.Status
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
                                     || p.Id.ToString(CultureInfo.InvariantCulture).Contains(term, StringComparison.OrdinalIgnoreCase)
                                     || p.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                     || p.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                     || GetStatus(p).Text.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedCategoryId.HasValue)
        {
            var selectedId = _selectedCategoryId.Value;
            var descendantIds = _categoryDescendants.GetValueOrDefault(selectedId) ?? new HashSet<int>();
            query = query.Where(p => p.CategoryId == selectedId || descendantIds.Contains(p.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(_selectedSupplierId))
        {
            query = query.Where(p => p.SupplierId == _selectedSupplierId);
        }

        if (!string.IsNullOrWhiteSpace(_selectedStatus))
        {
            query = query.Where(p => string.Equals(GetStatus(p).Text, _selectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        _filteredProducts = query
            .OrderByDescending(p => p.Status == ProductStatus.PendingApproval)
            .ThenBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToList();

        _currentPage = 1;
        ApplyPagination();
    }

    private void ApplyPagination()
    {
        _totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredProducts.Count / _pageSize));
        var pageIndex = Math.Clamp(_currentPage, 1, _totalPages);
        _currentPage = pageIndex;

        _pagedProducts = _filteredProducts
            .Skip((pageIndex - 1) * _pageSize)
            .Take(_pageSize)
            .ToList();
    }

    private void BuildCategoryOptions(List<Category> categories)
    {
        _categoryDescendants.Clear();

        var childrenLookup = categories.ToLookup(c => c.ParentId);

        HashSet<int> GetDescendants(int parentId)
        {
            var descendants = new HashSet<int>();
            foreach (var child in childrenLookup[parentId])
            {
                descendants.Add(child.Id);
                foreach (var grandChild in GetDescendants(child.Id))
                {
                    descendants.Add(grandChild);
                }
            }

            return descendants;
        }

        foreach (var category in categories)
        {
            _categoryDescendants[category.Id] = GetDescendants(category.Id);
        }

        _categoryOptions.Clear();

        void AddCategory(Category category, int depth)
        {
            _categoryOptions.Add(new CategoryOption(category.Id, category.DisplayName, depth));

            foreach (var child in childrenLookup[category.Id].OrderBy(c => c.DisplayName))
            {
                AddCategory(child, depth + 1);
            }
        }

        foreach (var root in childrenLookup[null].OrderBy(c => c.DisplayName))
        {
            AddCategory(root, 0);
        }
    }

    private void BuildStatusOptions()
    {
        _statusOptions = _allProducts
            .Select(p => GetStatus(p).Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(text => text)
            .Select(text => new StatusOption(text, text))
            .ToList();
    }

    private (string Text, string CssClass) GetStatus(ProductListItem product)
    {
        if (product.Status == ProductStatus.Approved && product.IsSuspendedBySupplier)
        {
            return ("Suspended", "bg-dark");
        }

        if (product.Status == ProductStatus.Approved && product.IsActive)
        {
            return ("Active", "bg-success");
        }

        if (product.Status == ProductStatus.Approved && !product.IsActive)
        {
            return ("Inactive", "bg-secondary");
        }

        if (product.Status == ProductStatus.PendingApproval && !product.IsActive && !product.IsSuspendedBySupplier)
        {
            return ("Pending", "bg-warning text-dark");
        }

        if (product.Status == ProductStatus.Rejected && !product.IsActive && !product.IsSuspendedBySupplier)
        {
            return ("Declined", "bg-danger");
        }

        return ("Unknown", "bg-secondary");
    }

    private void OnEditProduct(ProductListItem product)
    {
        NavigationManager.NavigateTo($"/products/{product.Id}");
    }

    private void PreviousPage()
    {
        if (_currentPage <= 1) return;
        _currentPage--;
        ApplyPagination();
    }

    private void NextPage()
    {
        if (_currentPage >= _totalPages) return;
        _currentPage++;
        ApplyPagination();
    }

    private async Task AddProductAsync()
    {
        _statusMessage = null;
        _errorMessage = null;

        try
        {
            var categoryId = SelectedCategoryId ?? _categoryOptions.FirstOrDefault()?.Id;
            var supplierId = SelectedSupplierId ?? _supplierOptions.FirstOrDefault()?.Id;

            if (categoryId is null || string.IsNullOrWhiteSpace(supplierId))
            {
                _errorMessage = "A category and supplier are required to create a product. Please add them first.";
                return;
            }

            var utcNow = DateTime.UtcNow;

            var product = new Product
            {
                Name = "New product",
                CategoryId = categoryId.Value,
                SupplierId = supplierId,
                ShortDescription = string.Empty,
                LongDescription = string.Empty,
                QuantityAvailable = 0,
                IsActive = false,
                IsFeatured = false,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
                BasePrice = 0.0,
                MarkupPercentage = 0.0,
                FinalPrice = 0.0,
                IsListingOnly = false,
                IsSuspendedBySupplier = false
            };

            await using var dbContext = await DbContextFactory.CreateDbContextAsync();

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            _statusMessage = "Product created. Redirecting...";
            _errorMessage = null;

            NavigationManager.NavigateTo($"/products/{product.Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to create product. {ex.Message}";
            _statusMessage = null;
        }
    }

    private static string GetSupplierDisplayName(ApplicationUser supplier)
    {
        return supplier.CompanyName
               ?? supplier.DisplayName
               ?? supplier.Email
               ?? supplier.UserName
               ?? supplier.Id;
    }

    private sealed record CategoryOption(int Id, string DisplayName, int Depth)
    {
        public string IndentedDisplayName => string.Concat(Enumerable.Repeat("\u00A0\u00A0", Depth)) + DisplayName;
    }

    private sealed record SupplierOption(string Id, string DisplayName);

    private sealed record StatusOption(string Value, string DisplayName);

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

        public ProductStatus Status { get; init; }
    }
}
