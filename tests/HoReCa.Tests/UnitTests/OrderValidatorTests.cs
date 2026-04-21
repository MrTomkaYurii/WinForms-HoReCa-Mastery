using FluentAssertions;
using FluentValidation.TestHelper;
using HoReCa.Application.DTOs;
using HoReCa.Application.Validators;
using Xunit;

namespace HoReCa.Tests.UnitTests;

public class OrderValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    [Fact]
    public void Validator_WhenWaiterNameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateOrderRequest(
            TableId: 1,
            BookingId: null,
            WaiterName: "",
            Items: new List<CreateOrderItemRequest>
            {
                new(MenuItemId: 1, Quantity: 2, ModifiersOrNotes: null)
            }
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WaiterName)
            .WithErrorMessage("Ім'я офіціанта є обов'язковим.");
    }

    [Fact]
    public void Validator_WhenItemsListIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateOrderRequest(
            TableId: 1,
            BookingId: null,
            WaiterName: "Олена",
            Items: new List<CreateOrderItemRequest>() // Empty!
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Замовлення має містити хоча б одну страву.");
    }

    [Fact]
    public void Validator_WhenItemQuantityIsZeroOrNegative_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateOrderRequest(
            TableId: 1,
            BookingId: null,
            WaiterName: "Олена",
            Items: new List<CreateOrderItemRequest>
            {
                new(MenuItemId: 5, Quantity: 0, ModifiersOrNotes: null)
            }
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Кількість має бути більше 0.");
    }

    [Fact]
    public void Validator_WhenOrderIsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var request = new CreateOrderRequest(
            TableId: 2,
            BookingId: null,
            WaiterName: "Максим",
            Items: new List<CreateOrderItemRequest>
            {
                new(MenuItemId: 1, Quantity: 1, ModifiersOrNotes: "Без солі"),
                new(MenuItemId: 3, Quantity: 2, ModifiersOrNotes: null)
            }
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
