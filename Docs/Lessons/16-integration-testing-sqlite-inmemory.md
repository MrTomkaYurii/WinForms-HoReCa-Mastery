# Практичний гайд 16: Інтеграційне тестування бізнес-сервісів через EF Core SQLite In-Memory

## 1. Теоретична база: Чому SQLite In-Memory замість EF Core InMemory Provider

При написанні інтеграційних тестів для сервісів (наприклад, [`TablePosService`](file:///C:/Users/Yurii/source/repos/- MASTERY/WinForms-HoReCa-Mastery/src/HoReCa.Infrastructure/Services/TablePosService.cs) або `RoomBookingService`) розробники часто роблять помилку, обираючи провайдер `UseInMemoryDatabase()`.

**Проблеми стандартного InMemory провайдера EF Core:**
- Він **не перевіряє зовнішні ключі (Foreign Keys)**. Ви можете зберегти рядок `OrderItem` з неіснуючим `MenuItemId`, і тест успішно пройде, але впаде в продакшені на реальному PostgreSQL.
- Він не підтримує транзакції (`BeginTransactionAsync()` падає або ігнорується).
- Не підтримує точність чисел `decimal` та SQL-функції.

**Правильне інженерне рішення: SQLite In-Memory:**
- Повноцінний реляційний SQL-рушій, що працює в оперативній пам'яті.
- Вимагає відкритого `SqliteConnection("DataSource=:memory:")`, який тримає базу активною протягом життя тесту.
- 100% перевірка зв'язків між таблицями, каскадного видалення та унікальних індексів.

---

## 2. Архітектурна схема ізоляції інтеграційного тесту

```mermaid
sequenceDiagram
    autonumber
    participant Test as Інтеграційний тест (xUnit)
    participant Conn as SqliteConnection ("DataSource=:memory:")
    participant Context as HoReCaDbContext
    participant Svc as TablePosService

    Test->>Conn: conn = new SqliteConnection("DataSource=:memory:")
    Test->>Conn: conn.Open() (База даних створена в RAM)
    Test->>Context: optionsBuilder.UseSqlite(conn).Options
    Test->>Context: context.Database.EnsureCreated() (Створення таблиць)
    Test->>Context: Посів початкових тестових даних (Стіл №1, 2 страви)
    
    Test->>Svc: svc = new TablePosService(factory)
    Test->>Svc: svc.CreateOrderAsync(tableId: 1, items)
    Svc->>Context: Збереження замовлення в RAM-базу
    
    Test->>Context: Прямий SQL-запит перевірки збереження замовлення
    Context-->>Test: Assert: Order збережено, статус столу = Occupied
    
    Test->>Conn: conn.Close(); conn.Dispose() (Повне видалення RAM-бази)
```

---

## 3. Детальний розбір реалізації в проєкті

### 3.1. Інтеграційний тест: `TablePosServiceTests.cs`

```csharp
public class TablePosServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<HoReCaDbContext> _options;

    public TablePosServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<HoReCaDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new HoReCaDbContext(_options);
        context.Database.EnsureCreated();

        // Початкові дані
        context.Tables.Add(new RestaurantTable { Id = 1, TableNumber = 1, Status = TableStatus.Free, Capacity = 4 });
        context.MenuItems.Add(new MenuItem { Id = 1, Name = "Стейк Рібай", Price = 380m, IsAvailable = true });
        context.SaveChanges();
    }

    [Fact]
    public async Task CreateOrderAsync_ValidItems_SavesOrderAndUpdatesTableStatus()
    {
        // Arrange
        var factory = new TestDbContextFactory(_options);
        var service = new TablePosService(factory);
        var items = new List<OrderItemRequestDto> { new(MenuItemId: 1, Quantity: 2, Notes: "Well-done") };

        // Act
        var result = await service.CreateOrderAsync(tableId: 1, items);

        // Assert
        result.Success.Should().BeTrue();
        
        await using var verifyContext = new HoReCaDbContext(_options);
        var table = await verifyContext.Tables.FindAsync(1);
        table!.Status.Should().Be(TableStatus.Occupied);

        var savedOrder = await verifyContext.Orders.Include(o => o.Items).FirstOrDefaultAsync();
        savedOrder.Should().NotBeNull();
        savedOrder!.Items.Should().HaveCount(1);
        savedOrder.Items[0].TotalPrice.Should().Be(760m); // 2 * 380 ₴
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
```

---

## 4. Підводні камені та типові помилки

> [!WARNING]
> **Життєвий цикл SqliteConnection:**
> База даних у пам'яті SQLite існує рівно стільки, скільки відкрито об'єкт `SqliteConnection`. Якщо ви закриєте конекшн або створите новий `DbContext` без передачі існуючого з'єднання, база буде миттєво знищена разом із усіма щойно збереженими даними.

---

## 5. Практичний чек-лист для закріплення
1. [ ] Перевірити наявність класу фікстури тесту з реалізацією `IDisposable`.
2. [ ] Переконатися, що перед виконанням тесту викликається `context.Database.EnsureCreated()`.
3. [ ] Перевірити тест на сценарій спроби створення замовлення для вже зайнятого столу.
