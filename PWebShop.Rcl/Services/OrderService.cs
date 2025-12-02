using System.Net.Http.Json;
using PWebShop.Rcl.Dtos;

namespace PWebShop.Rcl.Services;

public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public OrderService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<OrderDto?> CreateOrder(OrderCreateDto dto)
    {
        await EnsureAuthHeader();
        var response = await _httpClient.PostAsJsonAsync("api/orders", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OrderDto>();
        }
        return null;
    }

    public async Task<OrderDto?> GetOrderById(int id)
    {
        await EnsureAuthHeader();
        return await _httpClient.GetFromJsonAsync<OrderDto>($"api/orders/{id}");
    }

    public async Task<IEnumerable<OrderDto>> GetOrders()
    {
        await EnsureAuthHeader();
        return await _httpClient.GetFromJsonAsync<IEnumerable<OrderDto>>("api/orders") ?? new List<OrderDto>();
    }

    public async Task<bool> SimulatePayment(int orderId, string paymentMethod)
    {
        await EnsureAuthHeader();
        var dto = new PaymentSimulationRequestDto { PaymentMethod = paymentMethod };
        var response = await _httpClient.PostAsJsonAsync($"api/orders/{orderId}/payments/simulate", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelOrder(int orderId)
    {
        await EnsureAuthHeader();
        var response = await _httpClient.PostAsync($"api/orders/{orderId}/cancel", null);
        return response.IsSuccessStatusCode;
    }

    private async Task EnsureAuthHeader()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}
