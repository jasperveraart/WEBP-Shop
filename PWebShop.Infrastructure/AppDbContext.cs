using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;

namespace PWebShop.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<SubCategory> SubCategories => Set<SubCategory>();

    public DbSet<AvailabilityMethod> AvailabilityMethods => Set<AvailabilityMethod>();

    public DbSet<ProductAvailability> ProductAvailabilities => Set<ProductAvailability>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

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

            entity.HasMany(c => c.SubCategories)
                .WithOne(sc => sc.Category)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.Property(sc => sc.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(sc => sc.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(sc => sc.Description)
                .HasMaxLength(1000);

            entity.Property(sc => sc.SortOrder)
                .HasDefaultValue(0);

            entity.HasMany(sc => sc.Products)
                .WithOne(p => p.SubCategory)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
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

            entity.Property(p => p.BasePrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.MarkupPercentage)
                .HasColumnType("decimal(5,2)");

            entity.Property(p => p.FinalPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(p => p.UpdatedAt)
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
    }
}
