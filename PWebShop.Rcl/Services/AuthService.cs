using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using PWebShop.Rcl.Dtos;

namespace PWebShop.Rcl.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(HttpClient httpClient,
                       AuthenticationStateProvider authenticationStateProvider,
                       ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
        _localStorage = localStorage;
    }

    public async Task<AuthResultDto> RegisterCustomer(RegisterCustomerRequestDto registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register-customer", registerRequest);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        return result ?? new AuthResultDto { Success = false, Message = "Registration failed." };
    }

    public async Task<AuthResultDto> RegisterSupplier(RegisterSupplierRequestDto registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register-supplier", registerRequest);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        return result ?? new AuthResultDto { Success = false, Message = "Registration failed." };
    }

    public async Task<AuthResultDto> Login(LoginRequestDto loginRequest)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (loginResponse != null)
            {
                await _localStorage.SetItemAsync("authToken", loginResponse.Token);
                ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserAuthentication(loginResponse.Token);
                return new AuthResultDto 
                { 
                    Success = true,
                    Roles = loginResponse.Roles.ToList()
                };
            }
        }
        
        // Try to read error message
        string errorMessage = "Login failed.";
        try 
        {
             errorMessage = await response.Content.ReadAsStringAsync();
             // If it's a JSON object with a message, try to parse it, otherwise use the string
             // But for now, simple string is often returned by API for errors like "Unauthorized"
        }
        catch {}

        return new AuthResultDto { Success = false, Message = errorMessage };
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserLogout();
    }

    public async Task<CurrentUserResponseDto?> GetCurrentUser()
    {
        await EnsureAuthHeader();
        try
        {
            return await _httpClient.GetFromJsonAsync<CurrentUserResponseDto>("api/auth/me");
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResultDto> UpdateProfile(UpdateProfileRequestDto request)
    {
        await EnsureAuthHeader();
        var response = await _httpClient.PutAsJsonAsync("api/auth/profile", request);
        
        if (response.IsSuccessStatusCode)
        {
             return new AuthResultDto { Success = true, Message = "Profile updated successfully." };
        }

        string errorMessage = "Update failed.";
        try 
        {
             errorMessage = await response.Content.ReadAsStringAsync();
        }
        catch {}

        return new AuthResultDto { Success = false, Message = errorMessage };
    }

    public async Task<AuthResultDto> ChangePassword(ChangePasswordRequestDto request)
    {
        await EnsureAuthHeader();
        var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);

        if (response.IsSuccessStatusCode)
        {
            return new AuthResultDto { Success = true, Message = "Password changed successfully." };
        }

        string errorMessage = "Password change failed.";
        try
        {
            errorMessage = await response.Content.ReadAsStringAsync();
        }
        catch { }

        return new AuthResultDto { Success = false, Message = errorMessage };
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
