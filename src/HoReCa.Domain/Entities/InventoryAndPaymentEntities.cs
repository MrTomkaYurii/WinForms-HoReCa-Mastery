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

    public bool IsLowStock => CurrentStock <= MinimumStock;
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
