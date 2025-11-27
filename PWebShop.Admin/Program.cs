using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PWebShop.Admin.Components;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;
using PWebShop.Infrastructure.Storage;
using AdminDbContext = PWebShop.Infrastructure.AppDbContext;
using ApplicationUser = PWebShop.Infrastructure.Identity.ApplicationUser;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<AdminDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AdminDbContext>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(ApplicationRoleNames.Administrator, ApplicationRoleNames.Employee)
        .Build();
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.Configure<ImageStorageOptions>(builder.Configuration.GetSection("ImageStorage"));
builder.Services.AddSingleton<ImageStoragePathProvider>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await context.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in ApplicationRoleNames.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = app.Configuration["DefaultAdmin:Email"] ?? "admin@pwebshop.local";
    var adminUserName = app.Configuration["DefaultAdmin:UserName"] ?? adminEmail;
    var adminPassword = app.Configuration["DefaultAdmin:Password"] ?? "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser is null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(adminUser, adminPassword);
    }

    var rolesToAssign = new[] { ApplicationRoleNames.Administrator };
    foreach (var role in rolesToAssign)
    {
        if (!await userManager.IsInRoleAsync(adminUser, role))
        {
            await userManager.AddToRoleAsync(adminUser, role);
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

var imageStorageProvider = app.Services.GetRequiredService<ImageStoragePathProvider>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageStorageProvider.GetRootPath()),
    RequestPath = imageStorageProvider.RequestPath
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
