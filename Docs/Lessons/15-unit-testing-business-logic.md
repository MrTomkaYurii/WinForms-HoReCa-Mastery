# Lesson 15: Unit Testing Domain & Application Business Logic

## 1. Overview

A critical requirement of modern enterprise architecture is full testability. By decoupling domain entities and application services from Windows Forms UI code, we can test calculations, validators, and service workflows rapidly with **xUnit** and **FluentAssertions**.

---

## 2. Test Project Architecture

The test suite is structured as:
```
tests/HoReCa.Tests/
├── UnitTests/
│   ├── OrderServiceTests.cs       # Entity calculations & discounts
│   ├── BookingValidatorTests.cs   # FluentValidation date/phone checks
│   ├── OrderValidatorTests.cs     # Order request validation rules
│   └── InventoryAndPrinterTests.cs# Low stock computation & fiscal string formatting
└── IntegrationTests/
    └── TablePosServiceTests.cs    # EF Core SQLite in-memory integration
```

---

## 3. Testing Domain Entity Calculations

Testing that business logic remains correct under discounts and item mutations:

```csharp
[Fact]
public void RecalculateTotals_WithDiscount_ShouldApplyDiscountCorrectly()
{
    // Arrange
    var order = new Order
    {
        OrderNumber = "ORD-TEST-002",
        DiscountPercent = 15m // 15% discount
    };

    order.Items.Add(new OrderItem
    {
        Quantity = 1,
        UnitPrice = 1000m,
        Status = OrderStatus.New
    });

    // Act
    order.RecalculateTotals();

    // Assert
    order.Subtotal.Should().Be(1000m);
    order.TotalAmount.Should().Be(850m); // 1000 - 15%
}
```

---

## 4. Testing FluentValidation Rules

Using `FluentValidation.TestHelper` to verify that invalid input combinations produce expected error messages:

```csharp
[Fact]
public void Validator_WhenCheckOutDateBeforeCheckIn_ShouldHaveValidationError()
{
    // Arrange
    var validator = new CreateBookingRequestValidator();
    var request = new CreateBookingRequest(
        RoomId: 1,
        GuestFullName: "Тарас Шевченко",
        GuestPhone: "+38 (067) 111-22-33",
        GuestEmail: "taras@example.com",
        PassportOrId: "AA123456",
        CheckInDate: DateTime.Today.AddDays(5),
        CheckOutDate: DateTime.Today.AddDays(3), // Invalid: before check-in!
        SpecialRequests: null
    );

    // Act
    var result = validator.TestValidate(request);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.CheckOutDate)
        .WithErrorMessage("Дата виїзду має бути пізнішою за дату заїзду.");
}
```

---

## 5. Running the Test Suite

Execute the tests via the .NET CLI:
```bash
dotnet test tests/HoReCa.Tests/HoReCa.Tests.csproj
```
All 16 unit and integration tests run in under 2 seconds.
