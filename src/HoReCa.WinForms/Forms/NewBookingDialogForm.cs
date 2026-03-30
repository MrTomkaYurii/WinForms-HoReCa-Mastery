using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Application.Validators;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Forms;

public class NewBookingDialogForm : Form
{
    private readonly IRoomBookingService _bookingService;
    private readonly ComboBox _cbRooms;
    private readonly TextBox _txtName;
    private readonly MaskedTextBox _txtPhone;
    private readonly TextBox _txtEmail;
    private readonly DateTimePicker _dtCheckIn;
    private readonly DateTimePicker _dtCheckOut;
    private readonly TextBox _txtNotes;
    private readonly ErrorProvider _errorProvider;
    private List<RoomDto> _rooms = new();

    public NewBookingDialogForm(IRoomBookingService bookingService, int? defaultRoomId = null, DateTime? defaultDate = null)
    {
        _bookingService = bookingService;

        Text = "Нове бронювання номера готелю";
        Size = new Size(480, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ThemePalette.BackgroundCard;
        ForeColor = ThemePalette.TextPrimary;
        Font = ThemePalette.BodyFont;

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        int y = 20;
        int lblWidth = 140;
        int inputWidth = 280;

        // Room
        Controls.Add(new Label { Text = "Номер готелю:", Location = new Point(20, y), AutoSize = true });
        _cbRooms = new ComboBox
        {
            Location = new Point(lblWidth + 20, y - 4),
            Size = new Size(inputWidth, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary
        };
        Controls.Add(_cbRooms);
        y += 42;

        // Guest Name
        Controls.Add(new Label { Text = "ПІБ гостя:", Location = new Point(20, y), AutoSize = true });
        _txtName = new TextBox
        {
            Location = new Point(lblWidth + 20, y - 4),
            Size = new Size(inputWidth, 30),
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary
        };
        Controls.Add(_txtName);
        y += 42;

        // Guest Phone
        Controls.Add(new Label { Text = "Телефон (маска):", Location = new Point(20, y), AutoSize = true });
        _txtPhone = new MaskedTextBox
        {
            Location = new Point(lblWidth + 20, y - 4),
            Size = new Size(inputWidth, 30),
            Mask = "+38 (000) 000-00-00",
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary
        };
        Controls.Add(_txtPhone);
        y += 42;

        // Guest Email
        Controls.Add(new Label { Text = "Email гостя:", Location = new Point(20, y), AutoSize = true });
        _txtEmail = new TextBox
        {
            Location = new Point(lblWidth + 20, y - 4),
            Size = new Size(inputWidth, 30),
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary
        };
        Controls.Add(_txtEmail);
        y += 42;

        // Dates
        Controls.Add(new Label { Text = "Дата заїзду:", Location = new Point(20, y), AutoSize = true });
        _dtCheckIn = new DateTimePicker
        {
            Location = new Point(lblWidth + 20, y - 4),
            Size = new Size(inputWidth, 30),
            Format = DateTimePickerFormat.Short,
            Value = defaultDate ?? DateTime.Today
        };
        Controls.Add(_dtCheckIn);
        y += 42;

        Controls.Add(new Label { Text = "Дата виїзду:", Location = new Point(20, y), AutoSize = true });
        _dtCheckOut = new DateTimePicker
        {
            Location = new Point(lblWidth + 20, y - 4),
            Size = new Size(inputWidth, 30),
            Format = DateTimePickerFormat.Short,
            Value = (defaultDate ?? DateTime.Today).AddDays(2)
        };
        Controls.Add(_dtCheckOut);
        y += 42;

        // Notes
        Controls.Add(new Label { Text = "Побажання:", Location = new Point(20, y), AutoSize = true });
        _txtNotes = new TextBox
        {
            Location = new Point(lblWidth + 20, y - 4),
            Size = new Size(inputWidth, 60),
            Multiline = true,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary
        };
        Controls.Add(_txtNotes);
        y += 75;

        // Buttons
        var btnSave = new Button
        {
            Text = "ЗБЕРЕГТИ БРОНЬ",
            Location = new Point(lblWidth + 20, y),
            Size = new Size(180, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveBookingAsync();

        var btnCancel = new Button
        {
            Text = "Скасувати",
            DialogResult = DialogResult.Cancel,
            Location = new Point(lblWidth + 210, y),
            Size = new Size(90, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundDark,
            ForeColor = ThemePalette.TextMuted,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ThemePalette.Border;

        Controls.AddRange(new Control[] { btnSave, btnCancel });

        Load += async (s, e) =>
        {
            _rooms = await _bookingService.GetAllRoomsAsync();
            _cbRooms.DataSource = _rooms;
            _cbRooms.DisplayMember = "RoomNumber";
            _cbRooms.ValueMember = "Id";

            if (defaultRoomId.HasValue)
            {
                _cbRooms.SelectedValue = defaultRoomId.Value;
            }
        };
    }

    private async Task SaveBookingAsync()
    {
        _errorProvider.Clear();

        if (_cbRooms.SelectedValue == null)
        {
            _errorProvider.SetError(_cbRooms, "Оберіть номер готелю");
            return;
        }

        var req = new CreateBookingRequest(
            (int)_cbRooms.SelectedValue,
            _txtName.Text.Trim(),
            _txtPhone.Text.Trim(),
            _txtEmail.Text.Trim(),
            null,
            _dtCheckIn.Value.Date,
            _dtCheckOut.Value.Date,
            _txtNotes.Text.Trim()
        );

        var validator = new CreateBookingRequestValidator();
        var validationResult = await validator.ValidateAsync(req);

        if (!validationResult.IsValid)
        {
            foreach (var err in validationResult.Errors)
            {
                if (err.PropertyName.Contains("Name")) _errorProvider.SetError(_txtName, err.ErrorMessage);
                else if (err.PropertyName.Contains("Phone")) _errorProvider.SetError(_txtPhone, err.ErrorMessage);
                else if (err.PropertyName.Contains("CheckOut")) _errorProvider.SetError(_dtCheckOut, err.ErrorMessage);
                else _errorProvider.SetError(_cbRooms, err.ErrorMessage);
            }
            return;
        }

        try
        {
            await _bookingService.CreateBookingAsync(req);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка створення бронювання: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
