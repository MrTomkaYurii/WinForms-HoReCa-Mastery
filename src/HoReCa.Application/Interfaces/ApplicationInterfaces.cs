using HoReCa.Application.DTOs;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;

namespace HoReCa.Application.Interfaces;

public interface ITablePosService
{
    Task<List<TableDto>> GetTablesByZoneAsync(int? zoneId = null, CancellationToken ct = default);
    Task<List<TableZone>> GetZonesAsync(CancellationToken ct = default);
    Task UpdateTablePositionAsync(UpdateTablePositionRequest request, CancellationToken ct = default);
    Task<List<MenuCategory>> GetMenuCategoriesAsync(CancellationToken ct = default);
    Task<List<MenuItemDto>> GetMenuItemsAsync(int? categoryId = null, CancellationToken ct = default);
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDto?> GetActiveOrderForTableAsync(int tableId, CancellationToken ct = default);
    Task CloseOrderAndPayAsync(int orderId, PaymentMethod method, decimal amountPaid, CancellationToken ct = default);
}

public interface IRoomBookingService
{
    Task<List<RoomDto>> GetAllRoomsAsync(CancellationToken ct = default);
    Task<List<BookingDto>> GetBookingsForDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task CheckInGuestAsync(int bookingId, CancellationToken ct = default);
    Task CheckOutGuestAsync(int bookingId, PaymentMethod paymentMethod, CancellationToken ct = default);
}

public interface IKitchenService
{
    Task<List<OrderDto>> GetActiveKitchenOrdersAsync(CancellationToken ct = default);
    Task UpdateOrderItemStatusAsync(int orderItemId, OrderStatus newStatus, CancellationToken ct = default);
    Task BumpOrderToNextStageAsync(int orderId, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<List<InventoryDto>> GetAllInventoryAsync(string? search = null, CancellationToken ct = default);
    Task AdjustStockAsync(int inventoryItemId, decimal quantityDelta, string reason, CancellationToken ct = default);
}

public interface IAnalyticsService
{
    Task<AnalyticsSummaryDto> GetExecutiveSummaryAsync(CancellationToken ct = default);
}

public interface IReceiptPrintService
{
    string GenerateTextReceipt(OrderDto order, string businessName, string cashierName);
    void PrintReceipt(OrderDto order, string businessName, string cashierName, bool previewOnly = true);
}

public interface IAuthService
{
    UserAccountDto? CurrentUser { get; }
    Task<AuthResult> LoginAsync(string usernameOrPin, string? password, CancellationToken ct = default);
    Task<AuthResult> QuickLoginAsync(UserRole role, CancellationToken ct = default);
    Task<AuthResult> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<bool> ChangePinAsync(int userId, string newPin, CancellationToken ct = default);
    Task<List<UserAccountDto>> GetDemoAccountsAsync(CancellationToken ct = default);
    void Logout();
}

public interface IPdfReportService
{
    Task<string> GenerateReceiptPdfAsync(OrderDto order, string cashierName, CancellationToken ct = default);
    Task<string> GeneratePurchaseOrderPdfAsync(PurchaseOrderDto po, CancellationToken ct = default);
    Task<string> GenerateZReportPdfAsync(AnalyticsSummaryDto summary, string cashierName, CancellationToken ct = default);
}

public interface IStaffMonitoringService
{
    Task<StaffDashboardDto> GetStaffDashboardAsync(CancellationToken ct = default);
    Task<List<StaffShiftDto>> GetActiveShiftsAsync(CancellationToken ct = default);
    Task<StaffShiftDto> ClockInAsync(int userId, string? station = null, CancellationToken ct = default);
    Task ClockOutAsync(int shiftId, CancellationToken ct = default);
}

public interface IProcurementService
{
    Task<List<SupplierDto>> GetSuppliersAsync(CancellationToken ct = default);
    Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken ct = default);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest req, CancellationToken ct = default);
    Task ReceivePurchaseOrderAsync(int purchaseOrderId, CancellationToken ct = default);
}

