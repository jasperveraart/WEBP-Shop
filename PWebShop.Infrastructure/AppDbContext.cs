using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Infrastructure;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AvailabilityMethod> AvailabilityMethods => Set<AvailabilityMethod>();

    public DbSet<ProductAvailability> ProductAvailabilities => Set<ProductAvailability>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Price> Prices => Set<Price>();

    public DbSet<Stock> Stocks => Set<Stock>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(1000);

            entity.Property(c => c.SortOrder)
                .HasDefaultValue(0);

            entity.HasMany(c => c.Children)
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.ShortDescription)
                .HasMaxLength(500);

            entity.Property(p => p.LongDescription)
                .HasMaxLength(4000);

            entity.Property(p => p.Status)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Stock)
                .WithOne(s => s.Product)
                .HasForeignKey<Stock>(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Price>(entity =>
        {
            entity.Property(pr => pr.BasePrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(pr => pr.MarkupPercentage)
                .HasColumnType("decimal(5,2)");

            entity.Property(pr => pr.FinalPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(pr => pr.IsCurrent)
                .HasDefaultValue(false);

            entity.HasOne(pr => pr.Product)
                .WithMany(p => p.Prices)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.Property(s => s.QuantityAvailable)
                .HasDefaultValue(0);

            entity.Property(s => s.LastUpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<AvailabilityMethod>(entity =>
        {
            entity.Property(a => a.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.Description)
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<ProductAvailability>(entity =>
        {
            entity.HasKey(pa => new { pa.ProductId, pa.AvailabilityMethodId });

            entity.HasOne(pa => pa.Product)
                .WithMany(p => p.ProductAvailabilities)
                .HasForeignKey(pa => pa.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pa => pa.AvailabilityMethod)
                .WithMany(am => am.ProductAvailabilities)
                .HasForeignKey(pa => pa.AvailabilityMethodId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.Property(pi => pi.Url)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(pi => pi.AltText)
                .HasMaxLength(500);

            entity.Property(pi => pi.SortOrder)
                .HasDefaultValue(0);

            entity.HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(o => o.ShippingAddress)
                .HasMaxLength(1000);

            entity.Property(o => o.Status)
                .HasConversion<string>();

            entity.Property(o => o.PaymentStatus)
                .HasConversion<string>();

            entity.HasMany(o => o.OrderLines)
                .WithOne(ol => ol.Order)
                .HasForeignKey(ol => ol.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Shipment)
                .WithOne(s => s.Order)
                .HasForeignKey<Shipment>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.Property(ol => ol.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(ol => ol.LineTotal)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(ol => ol.Product)
                .WithMany()
                .HasForeignKey(ol => ol.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.PaymentMethod)
                .HasMaxLength(100);

            entity.Property(p => p.Status)
                .HasConversion<string>();
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.Property(s => s.Carrier)
                .HasMaxLength(200);

            entity.Property(s => s.TrackingCode)
                .HasMaxLength(200);

            entity.Property(s => s.Status)
                .HasConversion<string>();
        });
    }
}
