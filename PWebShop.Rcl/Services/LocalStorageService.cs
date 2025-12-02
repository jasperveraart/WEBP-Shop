using Microsoft.JSInterop;
using System.Text.Json;

namespace PWebShop.Rcl.Services;

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T> GetItemAsync<T>(string key)
    {
        var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);

        if (json == null)
            return default;

        try 
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            // If it's a simple string and T is string, return it directly (handling potential quotes if needed, but usually localStorage stores strings)
            // But JsonSerializer expects JSON. If we stored a raw string without quotes, Deserialize might fail if T is string.
            // Let's assume we always serialize when setting.
            if (typeof(T) == typeof(string))
            {
                return (T)(object)json;
            }
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }
}
