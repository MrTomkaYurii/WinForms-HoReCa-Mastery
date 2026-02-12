using HoReCa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HoReCa.Infrastructure.Persistence;

public class HoReCaDbContext : DbContext
{
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomBooking> Bookings => Set<RoomBooking>();
    public DbSet<TableZone> TableZones => Set<TableZone>();
    public DbSet<RestaurantTable> Tables => Set<RestaurantTable>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    public HoReCaDbContext(DbContextOptions<HoReCaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Guest
        modelBuilder.Entity<Guest>(b =>
        {
            b.HasKey(g => g.Id);
            b.Property(g => g.FullName).HasMaxLength(150).IsRequired();
            b.Property(g => g.Phone).HasMaxLength(30).IsRequired();
            b.Property(g => g.Email).HasMaxLength(100);
            b.Property(g => g.PassportOrId).HasMaxLength(50);
        });

        // Room
        modelBuilder.Entity<Room>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.RoomNumber).HasMaxLength(20).IsRequired();
            b.Property(r => r.DailyRate).HasPrecision(12, 2);
            b.HasIndex(r => r.RoomNumber).IsUnique();
        });

        // RoomBooking
        modelBuilder.Entity<RoomBooking>(b =>
        {
            b.HasKey(rb => rb.Id);
            b.Property(rb => rb.TotalPrice).HasPrecision(12, 2);

            b.HasOne(rb => rb.Guest)
                .WithMany(g => g.Bookings)
                .HasForeignKey(rb => rb.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(rb => rb.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(rb => rb.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TableZone
        modelBuilder.Entity<TableZone>(b =>
        {
            b.HasKey(z => z.Id);
            b.Property(z => z.Name).HasMaxLength(100).IsRequired();
        });

        // RestaurantTable
        modelBuilder.Entity<RestaurantTable>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasOne(t => t.Zone)
                .WithMany(z => z.Tables)
                .HasForeignKey(t => t.ZoneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MenuCategory
        modelBuilder.Entity<MenuCategory>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
        });

        // MenuItem
        modelBuilder.Entity<MenuItem>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Name).HasMaxLength(150).IsRequired();
            b.Property(m => m.Price).HasPrecision(12, 2);

            b.HasOne(m => m.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
            b.Property(o => o.Subtotal).HasPrecision(12, 2);
            b.Property(o => o.DiscountPercent).HasPrecision(5, 2);
            b.Property(o => o.TotalAmount).HasPrecision(12, 2);

            b.HasOne(o => o.Table)
                .WithMany(t => t.Orders)
                .HasForeignKey(o => o.TableId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(o => o.Booking)
                .WithMany()
                .HasForeignKey(o => o.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(b =>
        {
            b.HasKey(oi => oi.Id);
            b.Property(oi => oi.UnitPrice).HasPrecision(12, 2);

            b.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(oi => oi.MenuItem)
                .WithMany(m => m.OrderItems)
                .HasForeignKey(oi => oi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Inventory
        modelBuilder.Entity<InventoryItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Name).HasMaxLength(150).IsRequired();
            b.Property(i => i.SKU).HasMaxLength(50).IsRequired();
            b.Property(i => i.CurrentStock).HasPrecision(12, 3);
            b.Property(i => i.MinimumStock).HasPrecision(12, 3);
            b.Property(i => i.CostPerUnit).HasPrecision(12, 2);
        });

        // Payment
        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Amount).HasPrecision(12, 2);

            b.HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
