using HoReCa.Domain.Enums;

namespace HoReCa.Application.DTOs;

public record TableDto(
    int Id,
    int TableNumber,
    int ZoneId,
    string ZoneName,
    int Capacity,
    TableStatus Status,
    TableShape Shape,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    int? ActiveOrderId,
    decimal? ActiveOrderTotal,
    DateTime? OccupiedSince
);

public record UpdateTablePositionRequest(
    int TableId,
    int NewX,
    int NewY
);

public record MenuItemDto(
    int Id,
    int CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal Price,
    int PreparationMinutes,
    bool IsAvailable,
    string? ImageUrl
);

public record OrderItemDto(
    int Id,
    int MenuItemId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? ModifiersOrNotes,
    OrderStatus Status
);

public record OrderDto(
    int Id,
    string OrderNumber,
    int? TableId,
    int? TableNumber,
    int? BookingId,
    string WaiterName,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountPercent,
    decimal TotalAmount,
    DateTime CreatedAt,
    List<OrderItemDto> Items
);

public record CreateOrderRequest(
    int? TableId,
    int? BookingId,
    string WaiterName,
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    int MenuItemId,
    int Quantity,
    string? ModifiersOrNotes
);

public record RoomDto(
    int Id,
    string RoomNumber,
    int Floor,
    RoomType Type,
    decimal DailyRate,
    int Capacity,
    RoomStatus Status,
    string? CurrentGuestName
);

public record CreateBookingRequest(
    int RoomId,
    string GuestFullName,
    string GuestPhone,
    string? GuestEmail,
    string? PassportOrId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    string? SpecialRequests
);

public record BookingDto(
    int Id,
    int RoomId,
    string RoomNumber,
    int GuestId,
    string GuestFullName,
    string GuestPhone,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    decimal TotalPrice,
    bool IsCheckedIn,
    bool IsCheckedOut,
    string? SpecialRequests
);

public record InventoryDto(
    int Id,
    string Name,
    string SKU,
    string Unit,
    decimal CurrentStock,
    decimal MinimumStock,
    decimal CostPerUnit,
    bool IsLowStock
);

public record AnalyticsSummaryDto(
    decimal TodayRevenue,
    int TodayOrdersCount,
    decimal AverageCheck,
    double HotelOccupancyPercent,
    List<DailyRevenuePointDto> RevenueTrends,
    List<TopSellingItemDto> TopDishes
);

public record DailyRevenuePointDto(
    DateTime Date,
    decimal RestaurantRevenue,
    decimal HotelRevenue
);

public record TopSellingItemDto(
    string Name,
    int QuantitySold,
    decimal TotalRevenue
);
