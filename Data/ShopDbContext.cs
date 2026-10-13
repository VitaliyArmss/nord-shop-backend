using Microsoft.EntityFrameworkCore;
using NordShop.Api.Models;

namespace NordShop.Api.Data;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DemoUser> Users => Set<DemoUser>();
    public DbSet<ShopOrder> Orders => Set<ShopOrder>();
    public DbSet<ShopOrderItem> OrderItems => Set<ShopOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DemoUser>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(10, 2);
        modelBuilder.Entity<ShopOrder>().Property(x => x.Total).HasPrecision(10, 2);
        modelBuilder.Entity<ShopOrderItem>().Property(x => x.UnitPrice).HasPrecision(10, 2);
        modelBuilder.Entity<ShopOrder>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ShopOrderItem>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
