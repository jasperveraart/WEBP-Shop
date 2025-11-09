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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasMaxLength(2000);

            entity.Property(p => p.BasePrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.MarkupPercentage)
                .HasColumnType("decimal(5,2)");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();
        });
    }
}