using FluentValidation;
using HoReCa.Application.DTOs;

namespace HoReCa.Application.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .WithMessage("Оберіть дійсний номер готелю.");

        RuleFor(x => x.GuestFullName)
            .NotEmpty().WithMessage("ПІБ гостя є обов'язковим.")
            .MinimumLength(3).WithMessage("ПІБ має містити щонайменше 3 символи.");

        RuleFor(x => x.GuestPhone)
            .NotEmpty().WithMessage("Номер телефону є обов'язковим.")
            .Matches(@"^(\+?38)?\s?\(?0\d{2}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}$")
            .WithMessage("Формат номера: +38 (0XX) XXX-XX-XX.");

        RuleFor(x => x.CheckInDate)
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(-1))
            .WithMessage("Дата заїзду не може бути в минулому.");

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Дата виїзду має бути пізнішою за дату заїзду.");
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.WaiterName)
            .NotEmpty().WithMessage("Ім'я офіціанта є обов'язковим.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Замовлення має містити хоча б одну страву.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MenuItemId).GreaterThan(0).WithMessage("Невірна страва.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Кількість має бути більше 0.");
        });
    }
}

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Логін є обов'язковим.")
            .MinimumLength(3).WithMessage("Логін має містити щонайменше 3 символи.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ПІБ є обов'язковим.")
            .MinimumLength(3).WithMessage("ПІБ має містити щонайменше 3 символи.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль є обов'язковим.")
            .MinimumLength(4).WithMessage("Пароль має містити щонайменше 4 символи.");

        RuleFor(x => x.PinCode)
            .NotEmpty().WithMessage("PIN-код є обов'язковим.")
            .Matches(@"^\d{4}$").WithMessage("PIN-код має складатися рівно з 4 цифр.");
    }
}

