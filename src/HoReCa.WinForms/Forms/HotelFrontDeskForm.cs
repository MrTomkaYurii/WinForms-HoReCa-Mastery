using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using HoReCa.WinForms.Controls;

namespace HoReCa.WinForms.Forms;

public class HotelFrontDeskForm : BaseForm
{
    private readonly IRoomBookingService _bookingService;
    private readonly RoomRackControl _rackControl;
    private readonly ListView _lvRooms;

    public HotelFrontDeskForm(IRoomBookingService bookingService)
    {
        _bookingService = bookingService;

        var pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(16, 10, 16, 10)
        };

        var lblTitle = new Label
        {
            Text = "ГОТЕЛЬНИЙ ФОНД & ШАХМАТКА БРОНЮВАНЬ",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.Primary,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        var flpActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnRefresh = new Button
        {
            Text = "🔄 Оновити",
            Size = new Size(110, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderColor = ThemePalette.Border;
        btnRefresh.Click += async (s, e) => await LoadDataAsync();

        var btnNewBooking = new Button
        {
            Text = "+ НОВЕ БРОНЮВАННЯ",
            Size = new Size(195, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Primary,
            ForeColor = ThemePalette.TextInverse,
            Font = ThemePalette.TitleFont,
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnNewBooking.FlatAppearance.BorderSize = 0;
        btnNewBooking.Click += async (s, e) => await OpenNewBookingDialogAsync();

        flpActions.Controls.Add(btnRefresh);
        flpActions.Controls.Add(btnNewBooking);

        pnlTopBar.Controls.Add(flpActions);
        pnlTopBar.Controls.Add(lblTitle);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 350,
            SplitterWidth = 4,
            BackColor = ThemePalette.Border
        };

        // Top: Room Rack Calendar Grid
        _rackControl = new RoomRackControl { Dock = DockStyle.Fill };
        _rackControl.BookingClicked += async (s, b) => await ShowBookingDetailsAsync(b);
        _rackControl.CellClicked += async (s, args) => await OpenNewBookingForRoomDateAsync(args.room, args.date);
        split.Panel1.Controls.Add(_rackControl);

        // Bottom: ListView Rooms Card/Tile View
        _lvRooms = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Tile,
            TileSize = new Size(210, 80),
            BackColor = ThemePalette.BackgroundDark,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(12)
        };
        _lvRooms.Columns.Add("Room", 200);

        var pnlBottomTitle = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(10, 6, 0, 0)
        };
        pnlBottomTitle.Controls.Add(new Label { Text = "СПИСОК НОМЕРІВ ТА СТАТУСИ (TILE VIEW):", Font = ThemePalette.SmallFont, ForeColor = ThemePalette.TextMuted, AutoSize = true });

        var pnlBottomWrapper = new Panel { Dock = DockStyle.Fill };
        pnlBottomWrapper.Controls.Add(_lvRooms);
        pnlBottomWrapper.Controls.Add(pnlBottomTitle);
        split.Panel2.Controls.Add(pnlBottomWrapper);

        Controls.Add(split);
        Controls.Add(pnlTopBar);

        Load += async (s, e) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var rooms = await _bookingService.GetAllRoomsAsync();
        var bookings = await _bookingService.GetBookingsForDateRangeAsync(DateTime.Today.AddDays(-3), DateTime.Today.AddDays(15));

        _rackControl.SetData(rooms, bookings);

        // Populate ListView Tile view
        _lvRooms.Items.Clear();
        foreach (var r in rooms)
        {
            var item = new ListViewItem(new[]
            {
                $"Номер {r.RoomNumber} ({r.Type})",
                $"Тариф: {r.DailyRate:F0} ₴/доба",
                $"Статус: {r.Status}"
            });
            item.Tag = r;
            _lvRooms.Items.Add(item);
        }
    }

    private async Task OpenNewBookingDialogAsync()
    {
        using var dlg = new NewBookingDialogForm(_bookingService);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            await LoadDataAsync();
        }
    }

    private async Task OpenNewBookingForRoomDateAsync(RoomDto room, DateTime date)
    {
        using var dlg = new NewBookingDialogForm(_bookingService, room.Id, date);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            await LoadDataAsync();
        }
    }

    private async Task ShowBookingDetailsAsync(BookingDto booking)
    {
        string status = booking.IsCheckedOut ? "Виселений" : (booking.IsCheckedIn ? "Проживає" : "Заброньовано");
        string message = $"Бронювання #{booking.Id}\nГість: {booking.GuestFullName}\nТелефон: {booking.GuestPhone}\nНомер: {booking.RoomNumber}\nЗаїзд: {booking.CheckInDate:dd.MM.yyyy} — Виїзд: {booking.CheckOutDate:dd.MM.yyyy}\nСума: {booking.TotalPrice:F2} ₴\nСтатус: {status}\n\nБажаєте змінити статус?";

        var res = MessageBox.Show(message, "Деталі бронювання", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (res == DialogResult.Yes && !booking.IsCheckedIn && !booking.IsCheckedOut)
        {
            await _bookingService.CheckInGuestAsync(booking.Id);
            await LoadDataAsync();
            MessageBox.Show("Гостя успішно поселено (Check-In)!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else if (res == DialogResult.Yes && booking.IsCheckedIn && !booking.IsCheckedOut)
        {
            await _bookingService.CheckOutGuestAsync(booking.Id, PaymentMethod.CreditCard);
            await LoadDataAsync();
            MessageBox.Show("Гостя успішно виселено (Check-Out) та сформовано рахунок!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
