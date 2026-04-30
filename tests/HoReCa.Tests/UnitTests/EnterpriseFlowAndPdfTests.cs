using System.IO;
using HoReCa.Application.DTOs;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Persistence;
using HoReCa.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HoReCa.Tests.UnitTests;

public class EnterpriseFlowAndPdfTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<HoReCaDbContext> _options;
    private readonly IDbContextFactory<HoReCaDbContext> _factory;

    public EnterpriseFlowAndPdfTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<HoReCaDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new HoReCaDbContext(_options);
        ctx.Database.EnsureCreated();
        DbSeeder.SeedAsync(ctx).GetAwaiter().GetResult();

        _factory = new TestDbContextFactory(_options);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private class TestDbContextFactory : IDbContextFactory<HoReCaDbContext>
    {
        private readonly DbContextOptions<HoReCaDbContext> _options;
        public TestDbContextFactory(DbContextOptions<HoReCaDbContext> options) => _options = options;
        public HoReCaDbContext CreateDbContext() => new HoReCaDbContext(_options);
        public Task<HoReCaDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(new HoReCaDbContext(_options));
    }

    [Fact]
    public async Task Recipe_AutomaticInventoryDeduction_WhenOrderIsCreated()
    {
        // Arrange
        var posService = new TablePosService(_factory);
        await using var db = await _factory.CreateDbContextAsync();

        var table = await db.Tables.FirstAsync();
        var ribeyeDish = await db.MenuItems
            .Include(m => m.RecipeItems)
            .FirstAsync(m => m.Name.Contains("Рібай"));

        var beefRecipe = ribeyeDish.RecipeItems.First();
        var beefInventory = await db.Inventory.FindAsync(beefRecipe.InventoryItemId);
        Assert.NotNull(beefInventory);

        decimal initialStock = beefInventory.CurrentStock;
        int orderQty = 2;
        decimal expectedDeduction = beefRecipe.QuantityNeeded * orderQty;

        // Act
        var req = new CreateOrderRequest(
            table.Id,
            null,
            "Тестовий офіціант",
            new List<CreateOrderItemRequest>
            {
                new(ribeyeDish.Id, orderQty, "Medium Rare")
            }
        );

        var order = await posService.CreateOrderAsync(req);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.KitchenPending, order.Status);

        await using var verifyDb = await _factory.CreateDbContextAsync();
        var updatedBeef = await verifyDb.Inventory.FindAsync(beefRecipe.InventoryItemId);
        Assert.NotNull(updatedBeef);
        Assert.Equal(initialStock - expectedDeduction, updatedBeef.CurrentStock);
    }

    [Fact]
    public async Task PdfReportService_GeneratesValidGuestReceiptPdf()
    {
        // Arrange
        var pdfService = new PdfReportService();
        var order = new OrderDto(
            Id: 999,
            OrderNumber: "ORD-TEST-PDF-001",
            TableId: 5,
            TableNumber: 5,
            BookingId: null,
            WaiterName: "Максим Іванов",
            Status: OrderStatus.Paid,
            Subtotal: 850m,
            DiscountPercent: 5m,
            TotalAmount: 807.50m,
            CreatedAt: DateTime.UtcNow,
            Items: new List<OrderItemDto>
            {
                new(1, 10, "Стейк Рібай", 1, 690m, 690m, "Medium", OrderStatus.Served),
                new(2, 20, "Лимонад Маракуя", 1, 160m, 160m, null, OrderStatus.Served)
            }
        );

        // Act
        var filePath = await pdfService.GenerateReceiptPdfAsync(order, "Максим Іванов");

        // Assert
        Assert.True(File.Exists(filePath));
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 500, "PDF file must not be empty.");
    }

    [Fact]
    public async Task PdfReportService_GeneratesValidPurchaseOrderPdf()
    {
        // Arrange
        var pdfService = new PdfReportService();
        var po = new PurchaseOrderDto(
            Id: 101,
            OrderNumber: "PO-2026-0042",
            SupplierId: 1,
            SupplierName: "ТОВ «М'ясний Дім Галичина»",
            OrderDate: DateTime.UtcNow,
            ExpectedDeliveryDate: DateTime.UtcNow.AddDays(2),
            Status: PurchaseOrderStatus.Sent,
            TotalAmount: 14500m,
            Notes: "Термінове постачання до вихідних",
            Items: new List<PurchaseOrderItemDto>
            {
                new(1, 10, "Яловича вирізка преміум", "INV-BEEF-01", "кг", 20m, 490m, 9800m),
                new(2, 11, "Стейк Рібай", "INV-BEEF-02", "кг", 10m, 470m, 4700m)
            }
        );

        // Act
        var filePath = await pdfService.GeneratePurchaseOrderPdfAsync(po);

        // Assert
        Assert.True(File.Exists(filePath));
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 1000, "Purchase Order PDF file must not be empty.");
    }

    [Fact]
    public async Task ProcurementService_CreateAndReceivePurchaseOrder_UpdatesStock()
    {
        // Arrange
        var procurement = new ProcurementService(_factory);
        await using var db = await _factory.CreateDbContextAsync();

        var supplier = await db.Suppliers.FirstAsync();
        var item = await db.Inventory.FirstAsync(i => i.SupplierId == supplier.Id);
        decimal initialStock = item.CurrentStock;
        decimal orderQty = 15m;

        var req = new CreatePurchaseOrderRequest(
            supplier.Id,
            DateTime.UtcNow.AddDays(1),
            "Тестова закупівля",
            new List<CreatePurchaseOrderItemRequest>
            {
                new(item.Id, orderQty, item.CostPerUnit)
            }
        );

        // Act 1: Create PO
        var po = await procurement.CreatePurchaseOrderAsync(req);
        Assert.NotNull(po);
        Assert.Equal(PurchaseOrderStatus.Sent, po.Status);
        Assert.Equal(orderQty * item.CostPerUnit, po.TotalAmount);

        // Act 2: Receive PO
        await procurement.ReceivePurchaseOrderAsync(po.Id);

        // Assert
        await using var verifyDb = await _factory.CreateDbContextAsync();
        var updatedItem = await verifyDb.Inventory.FindAsync(item.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal(initialStock + orderQty, updatedItem.CurrentStock);

        var updatedPo = await verifyDb.PurchaseOrders.FindAsync(po.Id);
        Assert.NotNull(updatedPo);
        Assert.Equal(PurchaseOrderStatus.Delivered, updatedPo.Status);
    }

    [Fact]
    public async Task StaffMonitoringService_GetStaffDashboard_ReturnsCrewAndWaiters()
    {
        // Arrange
        var staffService = new StaffMonitoringService(_factory);

        // Act
        var dashboard = await staffService.GetStaffDashboardAsync();

        // Assert
        Assert.NotNull(dashboard);
        Assert.True(dashboard.TotalStaffOnShift > 0);
        Assert.True(dashboard.KitchenCrewCount > 0);
        Assert.True(dashboard.WaitersOnFloorCount > 0);
        Assert.NotEmpty(dashboard.KitchenStaff);
        Assert.NotEmpty(dashboard.Waiters);
    }
}
