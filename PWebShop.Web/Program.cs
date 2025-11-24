using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PWebShop.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient direct naar jouw API
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5091") });

await builder.Build().RunAsync();