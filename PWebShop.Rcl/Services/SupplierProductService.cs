using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using PWebShop.Rcl.Dtos;

namespace PWebShop.Rcl.Services;

public class SupplierProductService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public SupplierProductService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task EnsureAuthHeader()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<SupplierProductSummaryDto>> GetProductsAsync()
    {
        try
        {
            await EnsureAuthHeader();
            return await _http.GetFromJsonAsync<List<SupplierProductSummaryDto>>("api/supplier/products") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching supplier products: {ex.Message}");
            return new List<SupplierProductSummaryDto>();
        }
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
    {
        try
        {
            await EnsureAuthHeader();
            return await _http.GetFromJsonAsync<ProductDetailDto>($"api/supplier/products/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching product {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<ProductDetailDto?> CreateProductAsync(ProductCreateDto dto)
    {
        try
        {
            await EnsureAuthHeader();
            var response = await _http.PostAsJsonAsync("api/supplier/products", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductDetailDto>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating product: {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating product: {ex.Message}");
            return null;
        }
    }

    public async Task<ProductDetailDto?> UpdateProductAsync(int id, ProductUpdateDto dto)
    {
        try
        {
            await EnsureAuthHeader();
            var response = await _http.PutAsJsonAsync($"api/supplier/products/{id}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductDetailDto>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error updating product: {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating product: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteProductAsync(int id)
    {
        try
        {
             await EnsureAuthHeader();
             var response = await _http.DeleteAsync($"api/supplier/products/{id}");
             if (response.IsSuccessStatusCode)
             {
                 return (true, null);
             }
             
             var error = await response.Content.ReadAsStringAsync();
             return (false, error);
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Error deleting product: {ex.Message}");
             return (false, ex.Message);
        }
    }

    public async Task<ProductImageDto?> UploadImageAsync(int productId, IBrowserFile file, bool isMain)
    {
        try
        {
            await EnsureAuthHeader();
            Console.WriteLine($"Uploading image for product {productId}...");
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // 10MB max
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            
            content.Add(fileContent, "File", GetCorrectFileName(file));
            content.Add(new StringContent(isMain.ToString()), "IsMain");
            content.Add(new StringContent(file.Name), "AltText"); 

            var response = await _http.PostAsync($"api/supplier/products/{productId}/images", content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Image upload successful.");
                return await response.Content.ReadFromJsonAsync<ProductImageDto>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Image upload failed. Status: {response.StatusCode}, Error: {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading image: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(int productId, int imageId)
    {
        try
        {
            await EnsureAuthHeader();
            var response = await _http.DeleteAsync($"api/supplier/products/{productId}/images/{imageId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting image: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetMainImageAsync(int productId, int imageId)
    {
        try
        {
            await EnsureAuthHeader();
            var response = await _http.PutAsync($"api/supplier/products/{productId}/images/{imageId}/main", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting main image: {ex.Message}");
            return false;
        }
    }

    public string GetProductImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            url = "/images/products/0/no-image.png";
        }

        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        // If relative, prepend base address
        var baseAddress = _http.BaseAddress?.ToString().TrimEnd('/');
        if (!string.IsNullOrEmpty(baseAddress) && url.StartsWith("/"))
        {
            return $"{baseAddress}{url}";
        }
        
        return url;
    }
    private string GetCorrectFileName(IBrowserFile file)
    {
        var name = file.Name;
        if (file.ContentType == "image/jpeg" && !name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return Path.ChangeExtension(name, ".jpg");
        }
        return name;
    }
}
