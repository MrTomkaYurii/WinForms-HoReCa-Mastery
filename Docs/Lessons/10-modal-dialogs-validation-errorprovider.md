# Практичний гайд 10: Модальні діалоги, валідація FluentValidation та ErrorProvider

## 1. Теоретична база та принципи валідації в enterprise UI

Помилки людського фактора під час внесення даних гостя (неправильний формат телефону, дата виїзду раніша за дату заїзду, відсутність обов'язкового ПІБ) спричиняють конфлікти броней та прямі фінансові збитки готелю.

Для надійної валідації в системі **LuxeResto** поєднано два рівні:
1. **Рівень застосунку (Application Layer):** Чисті декларативні валідатори **FluentValidation**, які перевіряють DTO-контракти незалежно від UI.
2. **Рівень представлення (WinForms UI):** Компонент `ErrorProvider`, який виводить червоні значки попередження та спливаючі підказки безпосередньо біля проблемного поля введення, не блокуючи роботу користувача нав'язливими `MessageBox`.

---

## 2. Архітектурна схема валідації діалогу

```mermaid
sequenceDiagram
    autonumber
    actor User as Портьє рецепції
    participant Dlg as CreateBookingDialog
    participant Val as CreateBookingRequestValidator
    participant EP as ErrorProvider
    participant Svc as RoomBookingService

    User->>Dlg: Вводить дату заїзду 2026-04-25 та дату виїзду 2026-04-20
    User->>Dlg: Натискає "ЗБЕРЕГТИ БРОНЬ"
    Dlg->>Val: ValidateAsync(request)
    Val-->>Dlg: ValidationResult (IsValid = false, Error: "Дата виїзду має бути пізнішою за дату заїзду")
    Dlg->>EP: SetError(dtpCheckOut, error.ErrorMessage)
    EP-->>User: Відображення червоного значка (!) та підказки біля календаря
    
    User->>Dlg: Виправляє дату виїзду на 2026-04-28
    Dlg->>Val: ValidateAsync(request)
    Val-->>Dlg: ValidationResult (IsValid = true)
    Dlg->>EP: Clear()
    Dlg->>Svc: CreateBookingAsync(request)
    Svc-->>Dlg: Успішно збережено
    Dlg->>Dlg: DialogResult = OK; Close()
```

---

## 3. Детальний розбір реалізації в проєкті

### 3.1. Валідатор FluentValidation: [`CreateBookingRequestValidator.cs`](file:///C:/Users/Yurii/source/repos/- MASTERY/WinForms-HoReCa-Mastery/src/HoReCa.Application/Validators/CreateBookingRequestValidator.cs)

```csharp
public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.GuestFullName)
            .NotEmpty().WithMessage("ПІБ гостя є обов'язковим для заповнення.")
            .MinimumLength(3).WithMessage("ПІБ має містити щонайменше 3 символи.");

        RuleFor(x => x.GuestPhone)
            .NotEmpty().WithMessage("Номер телефону є обов'язковим.")
            .Matches(@"^\+?[0-9]{10,14}$").WithMessage("Введіть коректний номер телефону.");

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Дата виїзду обов'язково має бути пізнішою за дату заїзду.");
    }
}
```

### 3.2. Модальне вікно з `ErrorProvider`: [`CreateBookingDialog.cs`](file:///C:/Users/Yurii/source/repos/- MASTERY/WinForms-HoReCa-Mastery/src/HoReCa.WinForms/Forms/CreateBookingDialog.cs)

```csharp
private async Task OnSaveClicked()
{
    _errorProvider.Clear();

    var request = new CreateBookingRequest(
        RoomId: _selectedRoomId,
        GuestFullName: _txtFullName.Text.Trim(),
        GuestPhone: _txtPhone.Text.Trim(),
        CheckInDate: _dtpCheckIn.Value.Date,
        CheckOutDate: _dtpCheckOut.Value.Date
    );

    var result = await _validator.ValidateAsync(request);
    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            Control target = error.PropertyName switch
            {
                nameof(request.GuestFullName) => _txtFullName,
                nameof(request.GuestPhone) => _txtPhone,
                nameof(request.CheckOutDate) => _dtpCheckOut,
                _ => _txtFullName
            };
            _errorProvider.SetError(target, error.ErrorMessage);
        }
        return;
    }

    DialogResult = DialogResult.OK;
    Close();
}
```

---

## 4. Підводні камені та типові помилки

> [!WARNING]
> **Блокування інтерфейсу модальними вікнами:**
> Завжди викликайте діалогові вікна через `dialog.ShowDialog(this)`, передаючи батьківське вікно як овнера. Це гарантує, що модальне вікно не загубиться за головним вікном у системному таскбарі Windows при випадковому кліку.

---

## 5. Практичний чек-лист для закріплення
1. [ ] Перевірити, що `_errorProvider.BlinkStyle` налаштовано на комфортний режим без надмірного миготіння.
2. [ ] Переконатися, що кнопка «Зберегти» має `DialogResult = DialogResult.None` до проходження валідації.
3. [ ] Перевірити роботу валідації на перетин дат (не можна забронювати номер, який уже зайнятий іншим гостем).
