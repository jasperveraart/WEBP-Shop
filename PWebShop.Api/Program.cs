using Microsoft.EntityFrameworkCore;
using PWebShop.Infrastructure;
using PWebShop.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


// controllers en swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// database automatisch aanmaken voor nu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        var homeDelivery = new AvailabilityMethod
        {
            Name = "HomeDelivery",
            DisplayName = "Home Delivery",
            Description = "Delivered directly to the customer's door",
            IsActive = true
        };

        var storePickup = new AvailabilityMethod
        {
            Name = "StorePickup",
            DisplayName = "Store Pickup",
            Description = "Collect the product in store",
            IsActive = true
        };

        var digitalDownload = new AvailabilityMethod
        {
            Name = "DigitalDownload",
            DisplayName = "Digital Download",
            Description = "Download the digital product immediately",
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

        var smartphone = new Product
        {
            Category = phones,
            SupplierId = 1,
            Name = "Smartphone X100",
            ShortDescription = "Flagship smartphone with triple camera setup",
            LongDescription = "The Smartphone X100 features a 6.5\" OLED display, 128GB storage, and a long-lasting battery.",
            BasePrice = 350m,
            MarkupPercentage = 25m,
            FinalPrice = Math.Round(350m + (350m * 25m / 100m), 2),
            Status = "Active",
            IsFeatured = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = homeDelivery },
                new() { AvailabilityMethod = storePickup }
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

        var ultrabook = new Product
        {
            Category = laptops,
            SupplierId = 2,
            Name = "Ultrabook Pro 14",
            ShortDescription = "Lightweight ultrabook with 14-hour battery life",
            LongDescription = "Ultrabook Pro 14 comes with a 14\" display, 16GB RAM, and 512GB SSD storage in a sleek aluminum body.",
            BasePrice = 750m,
            MarkupPercentage = 20m,
            FinalPrice = Math.Round(750m + (750m * 20m / 100m), 2),
            Status = "Active",
            IsFeatured = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = homeDelivery },
                new() { AvailabilityMethod = storePickup }
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

        var produceBox = new Product
        {
            Category = produce,
            SupplierId = 3,
            Name = "Weekly Organic Produce Box",
            ShortDescription = "Seasonal organic fruits and vegetables",
            LongDescription = "A curated selection of seasonal organic produce delivered weekly to your doorstep.",
            BasePrice = 25m,
            MarkupPercentage = 30m,
            FinalPrice = Math.Round(25m + (25m * 30m / 100m), 2),
            Status = "Active",
            IsFeatured = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = homeDelivery },
                new() { AvailabilityMethod = storePickup }
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

        var ebook = new Product
        {
            Category = laptops,
            SupplierId = 4,
            Name = "Developer Productivity eBook",
            ShortDescription = "Guide to improving developer workflows",
            LongDescription = "An in-depth guide covering best practices and tools to enhance software developer productivity.",
            BasePrice = 15m,
            MarkupPercentage = 10m,
            FinalPrice = Math.Round(15m + (15m * 10m / 100m), 2),
            Status = "Active",
            IsFeatured = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = digitalDownload }
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

        db.AvailabilityMethods.AddRange(homeDelivery, storePickup, digitalDownload);
        db.Categories.AddRange(electronics, groceries, phones, laptops, produce);
        db.Products.AddRange(smartphone, ultrabook, produceBox, ebook);

        db.SaveChanges();
    }
}

// middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// later komen hier authentication en authorization
app.MapControllers();

// eenvoudige test endpoint
app.MapGet("/api/ping", () => Results.Ok(new { message = "Api is alive" }));

app.Run();
