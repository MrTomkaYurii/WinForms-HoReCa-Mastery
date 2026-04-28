# Lesson 16: Integration Testing with EF Core & SQLite In-Memory

## 1. Overview

Unit tests with mocks are valuable, but testing real database behavior (indexes, foreign key cascades, migrations, and LINQ translations) requires an actual relational database engine. In this lesson, we write automated integration tests using **SQLite in-memory mode** (`DataSource=:memory:`).

---

## 2. In-Memory SQLite Setup Pattern

In SQLite, an in-memory database exists only as long as its underlying physical `SqliteConnection` stays open. We open the connection once in the test fixture's constructor and pass it into `DbContextOptionsBuilder`:

```csharp
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

        // Seed initial reference tables
        SeedInitialData(initContext);

        _service = new TablePosService(new TestDbContextFactory(_options));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
```

---

## 3. Verifying End-to-End Service Workflows

```csharp
[Fact]
public async Task CloseOrderAndPayAsync_ShouldMarkOrderPaidAndFreeTable()
{
    // Arrange: Create an active order on Table #10
    using var context = new HoReCaDbContext(_options);
    var table = await context.Tables.FirstAsync();
    var item = await context.MenuItems.FirstAsync();

    var orderDto = await _service.CreateOrderAsync(new CreateOrderRequest(
        table.Id, null, "Ігор", new List<CreateOrderItemRequest> { new(item.Id, 1, null) }));

    // Act: Settle the bill with Credit Card
    await _service.CloseOrderAndPayAsync(orderDto.Id, PaymentMethod.CreditCard, 45m);

    // Assert: Check database state in a fresh DbContext
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
```

---

## 4. Key Takeaways

1. **Isolation**: Every test class execution gets its own pristine in-memory database instance, guaranteeing zero test pollution.
2. **Factory Emulation**: The lightweight `TestDbContextFactory` perfectly mirrors the production `IDbContextFactory` registered by Generic Host.
