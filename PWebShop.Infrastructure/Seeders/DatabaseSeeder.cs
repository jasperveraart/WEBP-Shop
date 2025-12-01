using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Infrastructure.Seeders;

public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public DatabaseSeeder(IServiceProvider serviceProvider, AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        await IdentitySeeder.SeedRolesAsync(_serviceProvider);

        var suppliers = await SeedUsersAsync();

        if (await _dbContext.Products.AnyAsync())
        {
            return;
        }

        await SeedCatalogAsync(suppliers);
        await SeedOrdersAsync();
    }

    private async Task<List<ApplicationUser>> SeedUsersAsync()
    {
        var suppliers = new List<ApplicationUser>();

        // Admin
        if (await _userManager.FindByEmailAsync("admin@example.com") == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@example.com",
                Email = "admin@example.com",
                DisplayName = "Admin User",
                EmailConfirmed = true,
                IsActive = true,
                IsAdministrator = true
            };
            await _userManager.CreateAsync(admin, "Admin123!");
            await _userManager.AddToRoleAsync(admin, ApplicationRoleNames.Administrator);
        }

        // Employee
        if (await _userManager.FindByEmailAsync("employee@example.com") == null)
        {
            var employee = new ApplicationUser
            {
                UserName = "employee@example.com",
                Email = "employee@example.com",
                DisplayName = "Employee User",
                EmailConfirmed = true,
                IsActive = true,
                IsEmployee = true
            };
            await _userManager.CreateAsync(employee, "Employee123!");
            await _userManager.AddToRoleAsync(employee, ApplicationRoleNames.Employee);
        }

        // Customer
        if (await _userManager.FindByEmailAsync("customer@example.com") == null)
        {
            var customer = new ApplicationUser
            {
                UserName = "customer@example.com",
                Email = "customer@example.com",
                DisplayName = "Customer User",
                EmailConfirmed = true,
                IsActive = true,
                IsCustomer = true
            };
            await _userManager.CreateAsync(customer, "Customer123!");
            await _userManager.AddToRoleAsync(customer, ApplicationRoleNames.Customer);
        }

        // Suppliers
        var supplierData = new[]
        {
            ("Label Stone", "label.stone@seed.local"),
            ("Label Blue", "label.blue@seed.local"),
            ("Studio Galaxy", "studio.galaxy@seed.local"),
            ("Studio River", "studio.river@seed.local")
        };

        foreach (var (name, email) in supplierData)
        {
            var supplier = await _userManager.FindByEmailAsync(email);
            if (supplier == null)
            {
                supplier = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    DisplayName = name,
                    EmailConfirmed = true,
                    IsActive = true,
                    IsSupplier = true
                };
                await _userManager.CreateAsync(supplier, "Supplier123!");
                await _userManager.AddToRoleAsync(supplier, ApplicationRoleNames.Supplier);
            }
            suppliers.Add(supplier);
        }

        return suppliers;
    }

    private async Task SeedCatalogAsync(List<ApplicationUser> suppliers)
    {
        var physicalShipping = new AvailabilityMethod
        {
            Name = "PhysicalShipping",
            DisplayName = "Physical (Shipping)",
            Description = "Physical CD/DVD orders shipped to customers",
            IsActive = true
        };

        var preOrder = new AvailabilityMethod
        {
            Name = "PreOrder",
            DisplayName = "Pre-Order",
            Description = "Reserve upcoming releases before they launch",
            IsActive = true
        };

        var collectorEdition = new AvailabilityMethod
        {
            Name = "CollectorEdition",
            DisplayName = "Collector's Edition",
            Description = "Limited collector's pressings with bonus artwork",
            IsActive = true
        };

        var digitalDownload = new AvailabilityMethod
        {
            Name = "DigitalDownload",
            DisplayName = "Digital Download",
            Description = "Downloadable liner notes and bonus tracks",
            IsActive = true
        };

        await _dbContext.AvailabilityMethods.AddRangeAsync(physicalShipping, preOrder, collectorEdition, digitalDownload);

        // Categories
        var music = new Category { Name = "music", DisplayName = "Music CDs", Description = "Compact discs across genres", SortOrder = 1, IsActive = true };
        var movies = new Category { Name = "movies", DisplayName = "DVD Movies", Description = "Feature films and box sets", SortOrder = 2, IsActive = true };
        var merch = new Category { Name = "merch", DisplayName = "Merchandise", Description = "Band t-shirts and posters", SortOrder = 3, IsActive = true };

        var classicRock = new Category { Parent = music, Name = "classic-rock", DisplayName = "Classic Rock", Description = "Iconic rock albums remastered on CD", SortOrder = 1, IsActive = true };
        var jazz = new Category { Parent = music, Name = "jazz", DisplayName = "Jazz", Description = "Timeless jazz recordings", SortOrder = 2, IsActive = true };
        var pop = new Category { Parent = music, Name = "pop", DisplayName = "Pop", Description = "Top 40 hits", SortOrder = 3, IsActive = true };
        
        var sciFi = new Category { Parent = movies, Name = "sci-fi", DisplayName = "Science Fiction", Description = "Space epics and time travel adventures", SortOrder = 1, IsActive = true };
        var dramas = new Category { Parent = movies, Name = "drama", DisplayName = "Drama", Description = "Award-winning dramas and classics", SortOrder = 2, IsActive = true };
        var action = new Category { Parent = movies, Name = "action", DisplayName = "Action", Description = "High octane action movies", SortOrder = 3, IsActive = true };

        var shirts = new Category { Parent = merch, Name = "shirts", DisplayName = "T-Shirts", Description = "Official band t-shirts", SortOrder = 1, IsActive = true };
        var posters = new Category { Parent = merch, Name = "posters", DisplayName = "Posters", Description = "Concert posters", SortOrder = 2, IsActive = true };

        await _dbContext.Categories.AddRangeAsync(music, movies, merch, classicRock, jazz, pop, sciFi, dramas, action, shirts, posters);

        var now = DateTime.UtcNow;
        var random = new Random();
        var products = new List<Product>();

        // Helper to create products
        void CreateProducts(Category category, int count, string namePrefix)
        {
            for (int i = 1; i <= count; i++)
            {
                var supplier = suppliers[random.Next(suppliers.Count)];
                var basePrice = random.Next(10, 50) + (random.NextDouble() > 0.5 ? 0.99 : 0.00);
                var markup = random.Next(15, 40);
                var finalPrice = Math.Round(basePrice + (basePrice * markup / 100.0), 2);
                
                var product = new Product
                {
                    Category = category,
                    SupplierId = supplier.Id,
                    Name = $"{namePrefix} Vol. {i}",
                    ShortDescription = $"A great addition to your {category.DisplayName} collection.",
                    LongDescription = $"This is a premium release of {namePrefix} Vol. {i}. It features high-quality production and exclusive content. A must-have for fans of {category.DisplayName}.",
                    IsFeatured = random.NextDouble() > 0.8,
                    IsActive = true,
                    Status = ProductStatus.Approved,
                    QuantityAvailable = random.Next(0, 100),
                    CreatedAt = now.AddDays(-random.Next(1, 365)),
                    UpdatedAt = now,
                    BasePrice = basePrice,
                    MarkupPercentage = markup,
                    FinalPrice = finalPrice,
                    ProductAvailabilities = new List<ProductAvailability>
                    {
                        new() { AvailabilityMethod = physicalShipping }
                    },
                    Images = new List<ProductImage>
                    {
                        new()
                        {
                            Url = $"https://picsum.photos/seed/{Guid.NewGuid()}/400/400",
                            AltText = $"{namePrefix} Vol. {i} Cover",
                            IsMain = true,
                            SortOrder = 1
                        }
                    }
                };

                if (random.NextDouble() > 0.7)
                {
                    product.ProductAvailabilities.Add(new ProductAvailability { AvailabilityMethod = digitalDownload });
                }

                products.Add(product);
            }
        }

        CreateProducts(classicRock, 8, "Rock Legends");
        CreateProducts(jazz, 8, "Smooth Jazz");
        CreateProducts(pop, 8, "Pop Hits");
        CreateProducts(sciFi, 5, "Galactic Wars");
        CreateProducts(dramas, 5, "Emotional Journey");
        CreateProducts(action, 5, "Explosive Action");
        CreateProducts(shirts, 10, "Band Tee");
        CreateProducts(posters, 10, "Tour Poster");

        await _dbContext.Products.AddRangeAsync(products);
        await _dbContext.SaveChangesAsync();
    }
    private async Task SeedOrdersAsync()
    {
        if (await _dbContext.Orders.AnyAsync())
        {
            return;
        }

        var customer = await _userManager.FindByEmailAsync("customer@example.com");
        if (customer == null) return;

        var products = await _dbContext.Products.ToListAsync();
        if (products.Count == 0) return;

        var random = new Random();
        var orders = new List<Order>();

        for (int i = 0; i < 15; i++)
        {
            var orderDate = DateTime.UtcNow.AddDays(-random.Next(1, 60));
            var orderLines = new List<OrderLine>();
            var itemCount = random.Next(1, 5);

            for (int j = 0; j < itemCount; j++)
            {
                var product = products[random.Next(products.Count)];
                var quantity = random.Next(1, 3);
                var unitPrice = (decimal)product.FinalPrice;
                
                orderLines.Add(new OrderLine
                {
                    ProductId = product.Id,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    LineTotal = unitPrice * quantity
                });
            }

            var totalAmount = orderLines.Sum(item => item.LineTotal);

            orders.Add(new Order
            {
                CustomerId = customer.Id,
                OrderDate = orderDate,
                TotalAmount = totalAmount,
                Status = (OrderStatus)random.Next(0, 4), // Pending, Processing, Shipped, Delivered
                OrderLines = orderLines,
                ShippingAddress = "123 Seed Street, Data City, 12345"
            });
        }

        await _dbContext.Orders.AddRangeAsync(orders);
        await _dbContext.SaveChangesAsync();
    }
}
