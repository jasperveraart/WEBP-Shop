using Microsoft.Extensions.Logging;
using Blazored.LocalStorage;

namespace PWebShop.Hybrid;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
        Console.WriteLine("MAUI_STARTUP: CreateMauiApp started");
#if ANDROID
        Android.Util.Log.Error("MAUI_STARTUP", "CreateMauiApp started");
#endif

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
#if ANDROID
            Android.Util.Log.Error("DOTNET_CRASH", $"Unhandled Exception: {error.ExceptionObject}");
#else
            Console.WriteLine($"[CRASH] Unhandled Exception: {error.ExceptionObject}");
#endif
        };

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
        Console.WriteLine("MAUI_STARTUP: AddMauiBlazorWebView done");

        // Auth
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, PWebShop.Rcl.Services.CustomAuthenticationStateProvider>();
        builder.Services.AddScoped<PWebShop.Rcl.Services.IAuthService, PWebShop.Rcl.Services.AuthService>();
        Console.WriteLine("MAUI_STARTUP: Auth services registered");

        // Services
        builder.Services.AddScoped<PWebShop.Rcl.Services.CartService>();
        builder.Services.AddScoped<PWebShop.Rcl.Services.ICategoryService, PWebShop.Rcl.Services.CategoryService>();
        builder.Services.AddScoped<PWebShop.Rcl.Services.IOrderService, PWebShop.Rcl.Services.OrderService>();
        Console.WriteLine("MAUI_STARTUP: Domain services registered");
        
        // LocalStorage (Custom implementation)
        builder.Services.AddScoped<PWebShop.Rcl.Services.ILocalStorageService, PWebShop.Rcl.Services.LocalStorageService>();
        Console.WriteLine("MAUI_STARTUP: Custom LocalStorage registered");

        // HttpClient
        builder.Services.AddScoped(sp => 
        {
            Console.WriteLine("MAUI_STARTUP: Creating HttpClient");
            var handler = new HttpClientHandler();
            // Bypass SSL certificate validation for local development on Android
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            
            var client = new HttpClient(handler);
            
            string baseAddress = "http://localhost:5091";
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Use Dev Tunnel URL for Android
                baseAddress = "https://mv7s0319-5091.uks1.devtunnels.ms";
            }
            
            client.BaseAddress = new Uri(baseAddress);
            Console.WriteLine($"MAUI_STARTUP: HttpClient created with base address {baseAddress}");
            return client;
        });

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        Console.WriteLine("MAUI_STARTUP: Building app");
		return builder.Build();
	}
}
