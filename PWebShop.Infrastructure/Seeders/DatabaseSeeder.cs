using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Infrastructure.Seeders;

public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppDbContext _dbContext;

    public DatabaseSeeder(IServiceProvider serviceProvider, AppDbContext dbContext)
    {
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        await IdentitySeeder.SeedRolesAsync(_serviceProvider);

        if (await _dbContext.Products.AnyAsync())
        {
            return;
        }

        await SeedCatalogAsync();
    }

    private async Task SeedCatalogAsync()
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

        var music = new Category
        {
            Name = "music",
            DisplayName = "Music CDs",
            Description = "Compact discs across genres",
            SortOrder = 1,
            IsActive = true
        };

        var movies = new Category
        {
            Name = "movies",
            DisplayName = "DVD Movies",
            Description = "Feature films and box sets",
            SortOrder = 2,
            IsActive = true
        };

        var classicRock = new Category
        {
            Parent = music,
            Name = "classic-rock",
            DisplayName = "Classic Rock",
            Description = "Iconic rock albums remastered on CD",
            SortOrder = 1,
            IsActive = true
        };

        var jazz = new Category
        {
            Parent = music,
            Name = "jazz",
            DisplayName = "Jazz",
            Description = "Timeless jazz recordings",
            SortOrder = 2,
            IsActive = true
        };

        var sciFi = new Category
        {
            Parent = movies,
            Name = "sci-fi",
            DisplayName = "Science Fiction",
            Description = "Space epics and time travel adventures",
            SortOrder = 1,
            IsActive = true
        };

        var dramas = new Category
        {
            Parent = movies,
            Name = "drama",
            DisplayName = "Drama",
            Description = "Award-winning dramas and classics",
            SortOrder = 2,
            IsActive = true
        };

        var now = DateTime.UtcNow;

        var rockBasePrice = 18m;
        var rockFinalPrice = Math.Round(rockBasePrice + (rockBasePrice * 20m / 100m), 2);
        var jazzBasePrice = 16m;
        var jazzFinalPrice = Math.Round(jazzBasePrice + (jazzBasePrice * 18m / 100m), 2);
        var sciFiBoxBasePrice = 32m;
        var sciFiBoxFinalPrice = Math.Round(sciFiBoxBasePrice + (sciFiBoxBasePrice * 22m / 100m), 2);
        var dramaBasePrice = 14m;
        var dramaFinalPrice = Math.Round(dramaBasePrice + (dramaBasePrice * 15m / 100m), 2);

        var rockAlbum = new Product
        {
            Category = classicRock,
            SupplierId = "label-stone",
            Name = "Legends of Rock: Remastered",
            ShortDescription = "Collector's CD with remastered tracks and poster",
            LongDescription = "A curated selection of remastered classic rock anthems, packaged with a fold-out tour poster and liner notes.",
            IsFeatured = true,
            IsActive = true,
            QuantityAvailable = 40,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)rockBasePrice,
            MarkupPercentage = 20,
            FinalPrice = (double)rockFinalPrice,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = physicalShipping },
                new() { AvailabilityMethod = collectorEdition }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/rock-remastered-cover.jpg",
                    AltText = "Legends of Rock CD cover",
                    IsMain = true,
                    SortOrder = 1
                },
                new()
                {
                    Url = "https://example.com/images/rock-remastered-poster.jpg",
                    AltText = "Fold-out poster included with Legends of Rock",
                    IsMain = false,
                    SortOrder = 2
                }
            }
        };

        var jazzAlbum = new Product
        {
            Category = jazz,
            SupplierId = "label-blue",
            Name = "Midnight Jazz Sessions",
            ShortDescription = "Late-night jazz standards with bonus live takes",
            LongDescription = "An intimate collection of jazz standards recorded live in studio, including downloadable liner notes and bonus solos.",
            IsFeatured = false,
            IsActive = true,
            QuantityAvailable = 65,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)jazzBasePrice,
            MarkupPercentage = 18,
            FinalPrice = (double)jazzFinalPrice,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = physicalShipping },
                new() { AvailabilityMethod = digitalDownload }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/midnight-jazz-sessions.jpg",
                    AltText = "Midnight Jazz Sessions album art",
                    IsMain = true,
                    SortOrder = 1
                }
            }
        };

        var sciFiBoxSet = new Product
        {
            Category = sciFi,
            SupplierId = "studio-galaxy",
            Name = "Galaxy Odyssey DVD Box Set",
            ShortDescription = "6-disc collector's box with behind-the-scenes features",
            LongDescription = "Experience the entire Galaxy Odyssey saga with restored visuals, commentary tracks, and a collectible slipcase.",
            IsFeatured = true,
            IsActive = true,
            QuantityAvailable = 20,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)sciFiBoxBasePrice,
            MarkupPercentage = 22,
            FinalPrice = (double)sciFiBoxFinalPrice,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = physicalShipping },
                new() { AvailabilityMethod = collectorEdition }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/galaxy-odyssey-box.jpg",
                    AltText = "Galaxy Odyssey DVD box set",
                    IsMain = true,
                    SortOrder = 1
                }
            }
        };

        var dramaFilm = new Product
        {
            Category = dramas,
            SupplierId = "studio-river",
            Name = "Riverside Stories (Director's Cut)",
            ShortDescription = "DVD release with deleted scenes and commentary",
            LongDescription = "A heartfelt drama following intertwined lives along the Riverside, presented with director commentary and cast interviews.",
            IsFeatured = false,
            IsActive = true,
            QuantityAvailable = 55,
            CreatedAt = now,
            UpdatedAt = now,
            BasePrice = (double)dramaBasePrice,
            MarkupPercentage = 15,
            FinalPrice = (double)dramaFinalPrice,
            ProductAvailabilities = new List<ProductAvailability>
            {
                new() { AvailabilityMethod = physicalShipping },
                new() { AvailabilityMethod = preOrder }
            },
            Images = new List<ProductImage>
            {
                new()
                {
                    Url = "https://example.com/images/riverside-stories-dvd.jpg",
                    AltText = "Riverside Stories DVD cover",
                    IsMain = true,
                    SortOrder = 1
                }
            }
        };

        await _dbContext.AvailabilityMethods.AddRangeAsync(physicalShipping, preOrder, collectorEdition, digitalDownload);
        await _dbContext.Categories.AddRangeAsync(music, movies, classicRock, jazz, sciFi, dramas);
        await _dbContext.Products.AddRangeAsync(rockAlbum, jazzAlbum, sciFiBoxSet, dramaFilm);

        await _dbContext.SaveChangesAsync();
    }
}
