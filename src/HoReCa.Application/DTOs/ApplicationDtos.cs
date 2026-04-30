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
    bool IsLowStock,
    int? SupplierId = null
);

public record AnalyticsSummaryDto(
    decimal TodayRevenue,
    int TodayOrdersCount,
    decimal AverageCheck,
    double HotelOccupancyPercent,
    List<DailyRevenuePointDto> RevenueTrends,
    List<TopSellingItemDto> TopDishes,
    List<TableActivityDto> TableActivities,
    List<WaiterPerformanceDto> WaiterStats,
    List<CategorySalesDto> CategorySales
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

public record TableActivityDto(
    int TableNumber,
    string ZoneName,
    int Capacity,
    int OrdersCount,
    decimal TotalRevenue,
    decimal AverageCheck,
    string Status
);

public record CategorySalesDto(
    string CategoryName,
    int QuantitySold,
    decimal TotalRevenue,
    double SharePercent
);

public record UserAccountDto(
    int Id,
    string Username,
    string FullName,
    string PinCode,
    UserRole Role,
    bool IsActive
);

public record LoginRequest(
    string UsernameOrPin,
    string? Password
);

public record RegisterUserRequest(
    string Username,
    string FullName,
    string Password,
    string PinCode,
    UserRole Role
);

public record AuthResult(
    bool Success,
    string? ErrorMessage,
    UserAccountDto? User
);

public record SupplierDto(
    int Id,
    string Name,
    string ContactPerson,
    string Phone,
    string Email,
    string Address,
    string Category,
    string Edrpou
);

public record PurchaseOrderItemDto(
    int Id,
    int InventoryItemId,
    string ItemName,
    string SKU,
    string Unit,
    decimal QuantityOrdered,
    decimal UnitCost,
    decimal LineTotal
);

public record PurchaseOrderDto(
    int Id,
    string OrderNumber,
    int SupplierId,
    string SupplierName,
    DateTime OrderDate,
    DateTime? ExpectedDeliveryDate,
    PurchaseOrderStatus Status,
    decimal TotalAmount,
    string? Notes,
    List<PurchaseOrderItemDto> Items
);

public record CreatePurchaseOrderItemRequest(
    int InventoryItemId,
    decimal QuantityOrdered,
    decimal UnitCost
);

public record CreatePurchaseOrderRequest(
    int SupplierId,
    DateTime? ExpectedDeliveryDate,
    string? Notes,
    List<CreatePurchaseOrderItemRequest> Items
);

public record StaffShiftDto(
    int Id,
    int UserId,
    string FullName,
    string Username,
    UserRole Role,
    DateTime ClockInTime,
    DateTime? ClockOutTime,
    int OrdersProcessed,
    decimal TotalSales,
    decimal TipsEarned,
    ShiftStatus Status,
    string? AssignedStation
);

public record ChefWorkloadDto(
    int UserId,
    string FullName,
    string Station,
    int ActiveOrdersCount,
    int CompletedTodayCount,
    bool IsOnShift
);

public record WaiterPerformanceDto(
    int UserId,
    string FullName,
    string AssignedZone,
    int ActiveTablesCount,
    int OrdersServedToday,
    decimal SalesToday,
    decimal TipsToday,
    bool IsOnShift
);

public record StaffDashboardDto(
    int TotalStaffOnShift,
    int KitchenCrewCount,
    int WaitersOnFloorCount,
    int ActiveOrdersInKitchen,
    List<ChefWorkloadDto> KitchenStaff,
    List<WaiterPerformanceDto> Waiters,
    List<StaffShiftDto> RecentShifts
);

public record UpdatePinRequest(
    int UserId,
    string CurrentPin,
    string NewPin
);

