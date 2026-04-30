using HoReCa.Domain.Common;
using HoReCa.Domain.Enums;

namespace HoReCa.Domain.Entities;

public class TableZone : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}

public class RestaurantTable : BaseEntity
{
    public int TableNumber { get; set; }
    public int ZoneId { get; set; }
    public TableZone Zone { get; set; } = null!;

    public int Capacity { get; set; } = 4;
    public TableStatus Status { get; set; } = TableStatus.Free;
    public TableShape Shape { get; set; } = TableShape.Rectangle;
    
    // Virtual Floor Coordinates for Drag & Drop UI
    public int PositionX { get; set; } = 50;
    public int PositionY { get; set; } = 50;
    public int Width { get; set; } = 90;
    public int Height { get; set; } = 70;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class MenuCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string IconKey { get; set; } = "Utensils";
    public int DisplayOrder { get; set; }
    public bool RouteToKitchen { get; set; } = true;

    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}

public class MenuItem : BaseEntity
{
    public int CategoryId { get; set; }
    public MenuCategory Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int PreparationMinutes { get; set; } = 15;
    public bool IsAvailable { get; set; } = true;
    public string? ImageUrl { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}

public class RecipeItem : BaseEntity
{
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;

    public decimal QuantityNeeded { get; set; }
}

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    
    public int? TableId { get; set; }
    public RestaurantTable? Table { get; set; }

    public int? BookingId { get; set; }
    public RoomBooking? Booking { get; set; }

    public string WaiterName { get; set; } = "Офіціант";
    public OrderStatus Status { get; set; } = OrderStatus.New;
    
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public void RecalculateTotals()
    {
        Subtotal = Items.Sum(i => i.Quantity * i.UnitPrice);
        decimal discountAmount = Subtotal * (DiscountPercent / 100m);
        TotalAmount = Math.Max(0, Subtotal - discountAmount);
    }
}

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? ModifiersOrNotes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
}
