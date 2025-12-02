using System.Net.Http.Json;
using PWebShop.Domain.Entities;

namespace PWebShop.Rcl.Services;

public class CategoryTreeDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<CategoryTreeDto> Children { get; set; } = new();
}

public interface ICategoryService
{
    Task<List<CategoryTreeDto>> GetCategoryTreeAsync();
}

public class CategoryService : ICategoryService
{
    private readonly HttpClient _httpClient;

    public CategoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CategoryTreeDto>> GetCategoryTreeAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<CategoryTreeDto>>("api/categories/tree");
            return result ?? new List<CategoryTreeDto>();
        }
        catch (Exception)
        {
            // Handle error or return empty list
            return new List<CategoryTreeDto>();
        }
    }
}
