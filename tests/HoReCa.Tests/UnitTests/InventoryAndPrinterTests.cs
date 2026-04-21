using FluentAssertions;
using HoReCa.Application.DTOs;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.Infrastructure.Services;
using Xunit;

namespace HoReCa.Tests.UnitTests;

public class InventoryAndPrinterTests
{
    [Fact]
    public void InventoryItem_WhenStockBelowMinimum_ShouldBeMarkedAsLowStock()
    {
        // Arrange
        var item = new InventoryItem
        {
            Name = "Кавові зерна",
            SKU = "INV-COFF-01",
            CurrentStock = 1.5m,
            MinimumStock = 5.0m
        };

        // Act & Assert
        item.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void InventoryItem_WhenStockAboveMinimum_ShouldNotBeLowStock()
    {
        // Arrange
        var item = new InventoryItem
        {
            Name = "Молоко 3.2%",
            SKU = "INV-MILK-01",
            CurrentStock = 25m,
            MinimumStock = 10m
        };

        // Act & Assert
        item.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public void ReceiptTextFormatter_GenerateTextReceipt_ShouldContainKeyFiscalFields()
    {
        // Arrange
        var order = new OrderDto(
            Id: 101,
            OrderNumber: "ORD-2026-TEST",
            TableId: 5,
            TableNumber: 5,
            BookingId: null,
            WaiterName: "Максим",
            Status: OrderStatus.Paid,
            Subtotal: 800m,
            DiscountPercent: 10m,
            TotalAmount: 720m,
            CreatedAt: new DateTime(2026, 3, 15, 19, 30, 0),
            Items: new List<OrderItemDto>
            {
                new(1, 10, "Стейк Рібай", 1, 600m, 600m, "Medium", OrderStatus.Paid),
                new(2, 20, "Бокал вина", 1, 200m, 200m, null, OrderStatus.Paid)
            }
        );

        // Act
        string text = ReceiptTextFormatter.GenerateTextReceipt(order, "GrandLux Restaurant", "Касир Ірина");

        // Assert
        text.Should().Contain("GRANDLUX RESTAURANT");
        text.Should().Contain("ORD-2026-TEST");
        text.Should().Contain("Стейк Рібай");
        text.Should().Contain("СТОЛИК: № 5");
        text.Should().Contain("720.00 ₴");
        text.Should().Contain("ФІСКАЛЬНИЙ ЧЕК");
    }
}
