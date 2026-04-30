using HoReCa.Domain.Common;
using HoReCa.Domain.Enums;

namespace HoReCa.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Unit { get; set; } = "кг";
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal CostPerUnit { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public bool IsLowStock => CurrentStock <= MinimumStock;
}

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Edrpou { get; set; } = string.Empty;

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;

    public decimal QuantityOrdered { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal => QuantityOrdered * UnitCost;
}

public class StaffShift : BaseEntity
{
    public int UserId { get; set; }
    public UserAccount User { get; set; } = null!;

    public DateTime ClockInTime { get; set; } = DateTime.UtcNow;
    public DateTime? ClockOutTime { get; set; }
    public UserRole RoleAtShift { get; set; }
    public int OrdersProcessed { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TipsEarned { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Active;
    public string? AssignedZoneOrStation { get; set; }
}

public class Payment : BaseEntity
{
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int? BookingId { get; set; }
    public RoomBooking? Booking { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
    public string? TransactionReference { get; set; }
}
