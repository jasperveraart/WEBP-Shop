using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure.Storage;

namespace PWebShop.Admin.Components.Pages;

public partial class ProductEdit : ComponentBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    [Parameter] public int Id { get; set; }

    [Inject] private AppDbContext DbContext { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private ImageStoragePathProvider ImageStoragePathProvider { get; set; } = default!;

    private ProductEditModel? _model;
    private List<CategoryOption> _categoryOptions = new();
    private List<SupplierOption> _supplierOptions = new();
    private List<ProductImageModel> _images = new();
    private IBrowserFile? _newImageFile;
    private string? _newImageName;
    private string? _newAltText;
    private bool _newIsMain;
    private string? _imageStatusMessage;
    private string? _imageErrorMessage;
    private bool _isUploadingImage;
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
                .Include(p => p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.Id))
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

            _images = product.Images
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.Id)
                .Select(i => new ProductImageModel(i.Id, i.Url, i.AltText, i.IsMain))
                .ToList();

            RecalculateFinalPrice();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load product. {ex.Message}";
            _images.Clear();
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

    private void OnNewImageSelected(InputFileChangeEventArgs args)
    {
        _newImageFile = args.File;
        _newImageName = _newImageFile?.Name;
        _imageErrorMessage = null;
        _imageStatusMessage = null;
    }

    private async Task UploadNewImageAsync()
    {
        _imageStatusMessage = null;
        _imageErrorMessage = null;

        if (_newImageFile is null)
        {
            _imageErrorMessage = "Please select an image file to upload.";
            return;
        }

        if (_newImageFile.Size > MaxImageSizeBytes)
        {
            _imageErrorMessage = "Image is too large. Maximum size is 5 MB.";
            return;
        }

        try
        {
            _isUploadingImage = true;

            var product = await DbContext.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (product is null)
            {
                _imageErrorMessage = "Product not found.";
                return;
            }

            var url = await SaveImageAsync(_newImageFile, product.Id);

            if (_newIsMain)
            {
                foreach (var existing in product.Images)
                {
                    existing.IsMain = false;
                }
            }

            var newImage = new ProductImage
            {
                ProductId = product.Id,
                Url = url,
                AltText = _newAltText ?? string.Empty,
                IsMain = _newIsMain
            };

            product.Images.Add(newImage);
            await DbContext.SaveChangesAsync();

            _images = product.Images
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.Id)
                .Select(i => new ProductImageModel(i.Id, i.Url, i.AltText, i.IsMain))
                .ToList();

            _imageStatusMessage = "Image uploaded successfully.";
            _newImageFile = null;
            _newImageName = null;
            _newAltText = null;
            _newIsMain = false;
        }
        catch (Exception ex)
        {
            _imageErrorMessage = $"Failed to upload image. {ex.Message}";
        }
        finally
        {
            _isUploadingImage = false;
        }
    }

    private async Task SetMainImageAsync(int imageId)
    {
        _imageStatusMessage = null;
        _imageErrorMessage = null;

        try
        {
            var images = await DbContext.ProductImages
                .Where(img => img.ProductId == Id)
                .ToListAsync();

            if (!images.Any(img => img.Id == imageId))
            {
                _imageErrorMessage = "Image not found.";
                return;
            }

            foreach (var image in images)
            {
                image.IsMain = image.Id == imageId;
            }

            await DbContext.SaveChangesAsync();

            _images = images
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.Id)
                .Select(i => new ProductImageModel(i.Id, i.Url, i.AltText, i.IsMain))
                .ToList();

            _imageStatusMessage = "Main image updated.";
        }
        catch (Exception ex)
        {
            _imageErrorMessage = $"Failed to update main image. {ex.Message}";
        }
    }

    private async Task DeleteImageAsync(int imageId)
    {
        _imageStatusMessage = null;
        _imageErrorMessage = null;

        try
        {
            var image = await DbContext.ProductImages
                .FirstOrDefaultAsync(img => img.Id == imageId && img.ProductId == Id);

            if (image is null)
            {
                _imageErrorMessage = "Image not found.";
                return;
            }

            DbContext.ProductImages.Remove(image);
            await DbContext.SaveChangesAsync();

            DeletePhysicalFile(image.Url);

            _images = await DbContext.ProductImages
                .Where(img => img.ProductId == Id)
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.Id)
                .Select(i => new ProductImageModel(i.Id, i.Url, i.AltText, i.IsMain))
                .ToListAsync();

            _imageStatusMessage = "Image deleted.";
        }
        catch (Exception ex)
        {
            _imageErrorMessage = $"Failed to delete image. {ex.Message}";
        }
    }

    private async Task<string> SaveImageAsync(IBrowserFile file, int productId)
    {
        var productFolder = ImageStoragePathProvider.GetProductFolder(productId);
        Directory.CreateDirectory(productFolder);

        var extension = Path.GetExtension(file.Name);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(productFolder, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.OpenReadStream(MaxImageSizeBytes).CopyToAsync(stream);
        }

        return ImageStoragePathProvider.BuildImageUrl(productId, fileName);
    }

    private void DeletePhysicalFile(string url)
    {
        var physicalPath = ImageStoragePathProvider.MapUrlToPhysicalPath(url);
        if (physicalPath is not null && System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
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

    private sealed record ProductImageModel(int Id, string Url, string? AltText, bool IsMain);
}
