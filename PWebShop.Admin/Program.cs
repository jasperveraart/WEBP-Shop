using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWebShop.Admin.Components;
using PWebShop.Admin.Data;
using PWebShop.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AdminDbContext>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies(options =>
    {
        options.ApplicationCookie.LoginPath = "/login";
        options.ApplicationCookie.AccessDeniedPath = "/login";
    });

builder.Services.AddAuthorizationBuilder()
    .AddFallbackPolicy(policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(ApplicationRoleNames.Administrator, ApplicationRoleNames.Employee);
    });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

    var rolesToAssign = new[] { ApplicationRoleNames.Administrator, ApplicationRoleNames.Employee };
    foreach (var role in rolesToAssign)
    {
        if (!await userManager.IsInRoleAsync(adminUser, role))
        {
            await userManager.AddToRoleAsync(adminUser, role);
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
