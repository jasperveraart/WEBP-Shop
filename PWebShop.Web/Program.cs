using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PWebShop.Rcl.Services;
using PWebShop.Web;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// HttpClient direct naar jouw API
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5091") });
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<SupplierProductService>();
builder.Services.AddScoped<SupplierOrderService>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();