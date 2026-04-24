using FluentAssertions;
using HoReCa.Application.DTOs;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Persistence;
using HoReCa.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HoReCa.Tests.IntegrationTests;

public class TablePosServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<HoReCaDbContext> _options;
    private readonly TablePosService _service;

    private class TestDbContextFactory(DbContextOptions<HoReCaDbContext> options) : IDbContextFactory<HoReCaDbContext>
    {
        public HoReCaDbContext CreateDbContext() => new(options);
        public Task<HoReCaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new HoReCaDbContext(options));
    }

    public TablePosServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<HoReCaDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initContext = new HoReCaDbContext(_options);
        initContext.Database.EnsureCreated();

        // Seed initial data
        var zone = new TableZone { Name = "Основний Зал", DisplayOrder = 1 };
        initContext.TableZones.Add(zone);
        initContext.SaveChanges();

        var table = new RestaurantTable
        {
            TableNumber = 10,
            ZoneId = zone.Id,
            Capacity = 4,
            Status = TableStatus.Free,
            PositionX = 100,
            PositionY = 150
        };
        initContext.Tables.Add(table);

        var category = new MenuCategory { Name = "Напої", DisplayOrder = 1 };
        initContext.MenuCategories.Add(category);
        initContext.SaveChanges();

        var item = new MenuItem
        {
            CategoryId = category.Id,
            Name = "Еспресо",
            Price = 45m,
            IsAvailable = true
        };
        initContext.MenuItems.Add(item);
        initContext.SaveChanges();

        _service = new TablePosService(new TestDbContextFactory(_options));
    }

    [Fact]
    public async Task GetTablesByZoneAsync_ShouldReturnSeededTables()
    {
        var tables = await _service.GetTablesByZoneAsync();
        tables.Should().NotBeEmpty();
        tables.Should().ContainSingle(t => t.TableNumber == 10);
    }

    [Fact]
    public async Task UpdateTablePositionAsync_ShouldPersistNewCoordinates()
    {
        using var context = new HoReCaDbContext(_options);
        var table = await context.Tables.FirstAsync();
        
        await _service.UpdateTablePositionAsync(new UpdateTablePositionRequest(table.Id, 250, 320));

        using var verifyContext = new HoReCaDbContext(_options);
        var updated = await verifyContext.Tables.FindAsync(table.Id);
        updated!.PositionX.Should().Be(250);
        updated.PositionY.Should().Be(320);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderAndSetTableOccupied()
    {
        using var context = new HoReCaDbContext(_options);
        var table = await context.Tables.FirstAsync();
        var item = await context.MenuItems.FirstAsync();

        var request = new CreateOrderRequest(
            TableId: table.Id,
            BookingId: null,
            WaiterName: "Ігор",
            Items: new List<CreateOrderItemRequest>
            {
                new(item.Id, 2, "Гарячий")
            }
        );

        var orderDto = await _service.CreateOrderAsync(request);

        orderDto.Should().NotBeNull();
        orderDto.TotalAmount.Should().Be(90m);
        orderDto.Items.Should().HaveCount(1);

        using var verifyContext = new HoReCaDbContext(_options);
        var updatedTable = await verifyContext.Tables.FindAsync(table.Id);
        updatedTable!.Status.Should().Be(TableStatus.Occupied);
    }

    [Fact]
    public async Task CloseOrderAndPayAsync_ShouldMarkOrderPaidAndFreeTable()
    {
        using var context = new HoReCaDbContext(_options);
        var table = await context.Tables.FirstAsync();
        var item = await context.MenuItems.FirstAsync();

        var orderDto = await _service.CreateOrderAsync(new CreateOrderRequest(
            table.Id, null, "Ігор", new List<CreateOrderItemRequest> { new(item.Id, 1, null) }));

        await _service.CloseOrderAndPayAsync(orderDto.Id, PaymentMethod.CreditCard, 45m);

        using var verifyContext = new HoReCaDbContext(_options);
        var updatedOrder = await verifyContext.Orders.FindAsync(orderDto.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Paid);

        var updatedTable = await verifyContext.Tables.FindAsync(table.Id);
        updatedTable!.Status.Should().Be(TableStatus.Free);

        var payment = await verifyContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderDto.Id);
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(45m);
        payment.Method.Should().Be(PaymentMethod.CreditCard);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
