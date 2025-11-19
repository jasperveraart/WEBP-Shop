using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PWebShop.Api.Application.Orders;
using PWebShop.Api.Application.Products;
using PWebShop.Api.Options;
using PWebShop.Api.Services;
using PWebShop.Domain.Entities;
using PWebShop.Domain.Services;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    options.User.RequireUniqueEmail = true)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// jwt
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtConfig.Key))
{
    throw new InvalidOperationException("Jwt:Key is not configured.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IOrderWorkflow, OrderWorkflow>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();

// controllers en swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PWebShop API",
        Version = "v1"
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Voer een geldig JWT bearer token in om beveiligde endpoints aan te roepen.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// database automatisch aanmaken voor nu
using (var scope = app.Services.CreateScope())
{
    var scopedProvider = scope.ServiceProvider;
    var db = scopedProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    await IdentitySeeder.SeedRolesAsync(scopedProvider);

    if (!await db.Products.AnyAsync())
    {
        var listingOnly = new AvailabilityMethod
        {
            Name = "ListingOnly",
            DisplayName = "Listing Only",
            Description = "Product is visible for marketing purposes only",
            IsActive = true
        };

        var forSaleShipping = new AvailabilityMethod
        {
            Name = "ForSaleShipping",
            DisplayName = "For Sale (Shipping)",
            Description = "Product can be purchased and shipped to the customer",
            IsActive = true
        };

        var downloadOnly = new AvailabilityMethod
        {
            Name = "DownloadOnly",
            DisplayName = "Download Only",
            Description = "Product is available exclusively through download",
            IsActive = true
        };

        var rent = new AvailabilityMethod
        {
            Name = "Rent",
            DisplayName = "Rent",
            Description = "Product can be rented for a limited period",
            IsActive = true
        };

        var electronics = new Category
        {
            Name = "electronics",
            DisplayName = "Electronics",
            Description = "Electronic gadgets and accessories",
            SortOrder = 1,
            IsActive = true
        };

        var groceries = new Category
        {
            Name = "groceries",
            DisplayName = "Groceries",
            Description = "Fresh food and pantry essentials",
            SortOrder = 2,
            IsActive = true
        };

        var phones = new Category
        {
            Parent = electronics,
            Name = "smartphones",
            DisplayName = "Smartphones",
            Description = "Latest smartphone models",
            SortOrder = 1,
            IsActive = true
        };

        var laptops = new Category
        {
            Parent = electronics,
            Name = "laptops",
            DisplayName = "Laptops",
            Description = "Portable computers and ultrabooks",
            SortOrder = 2,
            IsActive = true
        };

        var produce = new Category
        {
            Parent = groceries,
            Name = "produce",
            DisplayName = "Fresh Produce",
            Description = "Locally sourced fruits and vegetables",
            SortOrder = 1,
            IsActive = true
        };

        var now = DateTime.UtcNow;

        var smartphoneBasePrice = 350m;
        var smartphoneFinalPrice = Math.Round(smartphoneBasePrice + (smartphoneBasePrice * 25m / 100m), 2);

        var smartphone = new Product
        {
            Category = phones,
            SupplierId = 1,
            Name = "Smartphone X100",
            ShortDescription = "Flagship smartphone with triple camera setup",
            LongDescription = "The Smartphone X100 features a 6.5\" OLED display, 128GB storage, and a long-lasting battery.",
            Status = "Active",
            IsFeatured = true,
            IsActive = true,
            QuantityAvailable = 25,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)smartphoneBasePrice,
            FinalPrice = (double)smartphoneFinalPrice,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = forSaleShipping }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/smartphone-x100-main.jpg",
                    AltText = "Smartphone X100 front view",
                    IsMain = true,
                    SortOrder = 1
                },
                new()
                {
                    Url = "https://example.com/images/smartphone-x100-side.jpg",
                    AltText = "Smartphone X100 side profile",
                    IsMain = false,
                    SortOrder = 2
                }
            }
        };

        smartphone.Prices.Add(new Price
        {
            BasePrice = smartphoneBasePrice,
            MarkupPercentage = 25m,
            FinalPrice = smartphoneFinalPrice,
            ValidFrom = now,
            IsCurrent = true
        });

        smartphone.Stock = new Stock
        {
            QuantityAvailable = 25,
            LastUpdatedAt = now
        };

        var ultrabookBasePrice = 750m;
        var ultrabookFinalPrice = Math.Round(ultrabookBasePrice + (ultrabookBasePrice * 20m / 100m), 2);

        var ultrabook = new Product
        {
            Category = laptops,
            SupplierId = 2,
            Name = "Ultrabook Pro 14",
            ShortDescription = "Lightweight ultrabook with 14-hour battery life",
            LongDescription = "Ultrabook Pro 14 comes with a 14\" display, 16GB RAM, and 512GB SSD storage in a sleek aluminum body.",
            Status = "Active",
            IsFeatured = false,
            IsActive = true,
            QuantityAvailable = 12,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)ultrabookBasePrice,
            FinalPrice = (double)ultrabookFinalPrice,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = forSaleShipping },
                new() { AvailabilityMethod = rent }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/ultrabook-pro-14.jpg",
                    AltText = "Ultrabook Pro 14 on desk",
                    IsMain = true,
                    SortOrder = 1
                }
            }
        };

        ultrabook.Prices.Add(new Price
        {
            BasePrice = ultrabookBasePrice,
            MarkupPercentage = 20m,
            FinalPrice = ultrabookFinalPrice,
            ValidFrom = now,
            IsCurrent = true
        });

        ultrabook.Stock = new Stock
        {
            QuantityAvailable = 12,
            LastUpdatedAt = now
        };

        var produceBoxBasePrice = 25m;
        var produceBoxFinalPrice = Math.Round(produceBoxBasePrice + (produceBoxBasePrice * 30m / 100m), 2);

        var produceBox = new Product
        {
            Category = produce,
            SupplierId = 3,
            Name = "Weekly Organic Produce Box",
            ShortDescription = "Seasonal organic fruits and vegetables",
            LongDescription = "A curated selection of seasonal organic produce delivered weekly to your doorstep.",
            Status = "Active",
            IsFeatured = false,
            IsActive = true,
            QuantityAvailable = 0,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)produceBoxBasePrice,
            FinalPrice = (double)produceBoxFinalPrice,
            IsListingOnly = true,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = listingOnly }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/organic-produce-box.jpg",
                    AltText = "Organic produce box",
                    IsMain = true,
                    SortOrder = 1
                }
            }
        };

        produceBox.Prices.Add(new Price
        {
            BasePrice = produceBoxBasePrice,
            MarkupPercentage = 30m,
            FinalPrice = produceBoxFinalPrice,
            ValidFrom = now,
            IsCurrent = true
        });

        produceBox.Stock = new Stock
        {
            QuantityAvailable = 100,
            LastUpdatedAt = now
        };

        var ebookBasePrice = 15m;
        var ebookFinalPrice = Math.Round(ebookBasePrice + (ebookBasePrice * 10m / 100m), 2);

        var ebook = new Product
        {
            Category = laptops,
            SupplierId = 4,
            Name = "Developer Productivity eBook",
            ShortDescription = "Guide to improving developer workflows",
            LongDescription = "An in-depth guide covering best practices and tools to enhance software developer productivity.",
            Status = "Active",
            IsFeatured = false,
            IsActive = true,
            QuantityAvailable = 250,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)ebookBasePrice,
            FinalPrice = (double)ebookFinalPrice,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = downloadOnly }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/developer-productivity-ebook.jpg",
                    AltText = "Developer productivity ebook cover",
                    IsMain = true,
                    SortOrder = 1
                }
            }
        };

        ebook.Prices.Add(new Price
        {
            BasePrice = ebookBasePrice,
            MarkupPercentage = 10m,
            FinalPrice = ebookFinalPrice,
            ValidFrom = now,
            IsCurrent = true
        });

        ebook.Stock = new Stock
        {
            QuantityAvailable = 250,
            LastUpdatedAt = now
        };

        await db.AvailabilityMethods.AddRangeAsync(listingOnly, forSaleShipping, downloadOnly, rent);
        await db.Categories.AddRangeAsync(electronics, groceries, phones, laptops, produce);
        await db.Products.AddRangeAsync(smartphone, ultrabook, produceBox, ebook);

        await db.SaveChangesAsync();
    }
}

// middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// eenvoudige test endpoint
app.MapGet("/api/ping", () => Results.Ok(new { message = "Api is alive" }));

app.Run();
