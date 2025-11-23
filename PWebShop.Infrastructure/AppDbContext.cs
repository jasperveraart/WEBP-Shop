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

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

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

            entity.Property(p => p.SupplierId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(p => p.BasePrice)
                .HasDefaultValue(0.0);

            entity.Property(p => p.MarkupPercentage)
                .HasDefaultValue(0.0);

            entity.Property(p => p.FinalPrice)
                .HasDefaultValue(0.0);

            entity.Property(p => p.IsListingOnly)
                .HasDefaultValue(false);

            entity.Property(p => p.IsSuspendedBySupplier)
                .HasDefaultValue(false);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Product_BasePrice_NonNegative",
                    "BasePrice >= 0");

                t.HasCheckConstraint(
                    "CK_Product_FinalPrice_NonNegative",
                    "FinalPrice >= 0");
            });

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Supplier)
                .WithMany()
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.ProductAvailabilities)
                .WithOne(pa => pa.Product)
                .HasForeignKey(pa => pa.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
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
    }
}
