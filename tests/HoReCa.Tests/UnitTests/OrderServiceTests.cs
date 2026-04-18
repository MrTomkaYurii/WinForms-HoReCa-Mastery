using FluentAssertions;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using Xunit;

namespace HoReCa.Tests.UnitTests;

public class OrderServiceTests
{
    [Fact]
    public void RecalculateTotals_WithoutDiscount_ShouldCalculateCorrectSubtotalAndTotal()
    {
        // Arrange
        var order = new Order
        {
            OrderNumber = "ORD-TEST-001",
            DiscountPercent = 0
        };

        order.Items.Add(new OrderItem
        {
            Quantity = 2,
            UnitPrice = 250m,
            Status = OrderStatus.New
        });

        order.Items.Add(new OrderItem
        {
            Quantity = 1,
            UnitPrice = 500m,
            Status = OrderStatus.New
        });

        // Act
        order.RecalculateTotals();

        // Assert
        order.Subtotal.Should().Be(1000m);
        order.TotalAmount.Should().Be(1000m);
    }

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
            Quantity = 2,
            UnitPrice = 500m,
            Status = OrderStatus.New
        });

        // Act
        order.RecalculateTotals();

        // Assert
        order.Subtotal.Should().Be(1000m);
        order.TotalAmount.Should().Be(850m);
    }
}
