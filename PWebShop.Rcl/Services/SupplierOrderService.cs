using System.Net.Http.Json;
using PWebShop.Domain.Entities;
using PWebShop.Rcl.Dtos;

namespace PWebShop.Rcl.Services;

public class SupplierOrderService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public SupplierOrderService(HttpClient http, ILocalStorageService localStorage)
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

    public async Task<List<SupplierOrderDto>> GetOrdersAsync()
    {
        try
        {
            await EnsureAuthHeader();
            return await _http.GetFromJsonAsync<List<SupplierOrderDto>>("api/supplier/orders") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching supplier orders: {ex.Message}");
            return new List<SupplierOrderDto>();
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        try
        {
            await EnsureAuthHeader();
            var response = await _http.PutAsJsonAsync($"api/supplier/orders/{orderId}/status", status);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating order status: {ex.Message}");
            return false;
        }
    }
}
