using FluentAssertions;
using FluentValidation.TestHelper;
using HoReCa.Application.DTOs;
using HoReCa.Application.Validators;
using Xunit;

namespace HoReCa.Tests.UnitTests;

public class BookingValidatorTests
{
    private readonly CreateBookingRequestValidator _validator = new();

    [Fact]
    public void Validator_WhenCheckOutDateBeforeCheckIn_ShouldHaveValidationError()
    {
        // Arrange
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
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CheckOutDate)
            .WithErrorMessage("Дата виїзду має бути пізнішою за дату заїзду.");
    }

    [Fact]
    public void Validator_WhenPhoneInvalid_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateBookingRequest(
            RoomId: 1,
            GuestFullName: "Іван Франко",
            GuestPhone: "invalid-phone",
            GuestEmail: null,
            PassportOrId: null,
            CheckInDate: DateTime.Today.AddDays(1),
            CheckOutDate: DateTime.Today.AddDays(3),
            SpecialRequests: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GuestPhone);
    }

    [Fact]
    public void Validator_WhenValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateBookingRequest(
            RoomId: 2,
            GuestFullName: "Леся Українка",
            GuestPhone: "+38 (050) 123-45-67",
            GuestEmail: "lesya@example.com",
            PassportOrId: "BB987654",
            CheckInDate: DateTime.Today.AddDays(2),
            CheckOutDate: DateTime.Today.AddDays(5),
            SpecialRequests: "Номер на верхньому поверсі"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
