using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Controls;

public class RoomRackControl : UserControl
{
    private List<RoomDto> _rooms = new();
    private List<BookingDto> _bookings = new();
    private DateTime _startDate = DateTime.Today.AddDays(-2);
    private int _daysCount = 14;

    private const int RoomColWidth = 120;
    private const int DayColWidth = 55;
    private const int RowHeight = 38;
    private const int HeaderHeight = 45;

    public event EventHandler<BookingDto>? BookingClicked;
    public event EventHandler<(RoomDto room, DateTime date)>? CellClicked;

    public RoomRackControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        DoubleBuffered = true;
        BackColor = ThemePalette.BackgroundDark;
        AutoScroll = true;
    }

    public void SetData(List<RoomDto> rooms, List<BookingDto> bookings, DateTime? start = null)
    {
        _rooms = rooms;
        _bookings = bookings;
        if (start.HasValue) _startDate = start.Value;
        
        AutoScrollMinSize = new Size(RoomColWidth + (_daysCount * DayColWidth) + 20, HeaderHeight + (_rooms.Count * RowHeight) + 20);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int clickX = e.X - AutoScrollPosition.X;
        int clickY = e.Y - AutoScrollPosition.Y;

        if (clickY < HeaderHeight || clickX < RoomColWidth) return;

        int rowIndex = (clickY - HeaderHeight) / RowHeight;
        int dayIndex = (clickX - RoomColWidth) / DayColWidth;

        if (rowIndex >= 0 && rowIndex < _rooms.Count && dayIndex >= 0 && dayIndex < _daysCount)
        {
            var room = _rooms[rowIndex];
            var date = _startDate.AddDays(dayIndex);

            // Check if clicked inside a booking
            var booking = _bookings.FirstOrDefault(b => b.RoomId == room.Id && date >= b.CheckInDate.Date && date < b.CheckOutDate.Date);
            if (booking != null)
            {
                BookingClicked?.Invoke(this, booking);
            }
            else
            {
                CellClicked?.Invoke(this, (room, date));
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        int totalWidth = RoomColWidth + (_daysCount * DayColWidth);
        int totalHeight = HeaderHeight + (_rooms.Count * RowHeight);

        // Draw header
        using var headerBrush = new SolidBrush(ThemePalette.BackgroundSidebar);
        g.FillRectangle(headerBrush, 0, 0, totalWidth, HeaderHeight);

        using var fontHeader = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fontDay = new Font("Segoe UI", 8f, FontStyle.Regular);
        using var textBrush = new SolidBrush(ThemePalette.TextPrimary);
        using var mutedBrush = new SolidBrush(ThemePalette.TextMuted);
        using var borderPen = new Pen(ThemePalette.Border);

        g.DrawString("НОМЕР / КАТ.", fontHeader, textBrush, 10, 14);

        for (int d = 0; d < _daysCount; d++)
        {
            var date = _startDate.AddDays(d);
            int x = RoomColWidth + (d * DayColWidth);

            bool isToday = date.Date == DateTime.Today;
            if (isToday)
            {
                using var todayBrush = new SolidBrush(Color.FromArgb(40, ThemePalette.Primary));
                g.FillRectangle(todayBrush, x, 0, DayColWidth, totalHeight);
            }

            g.DrawLine(borderPen, x, 0, x, totalHeight);
            string dayTitle = date.ToString("dd.MM");
            string dayOfWeek = date.ToString("ddd");

            g.DrawString(dayTitle, fontHeader, isToday ? new SolidBrush(ThemePalette.Primary) : textBrush, x + 8, 8);
            g.DrawString(dayOfWeek, fontDay, mutedBrush, x + 14, 25);
        }

        // Draw Rows
        using var rowFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var typeFont = new Font("Segoe UI", 7.5f, FontStyle.Regular);

        for (int r = 0; r < _rooms.Count; r++)
        {
            int y = HeaderHeight + (r * RowHeight);
            var room = _rooms[r];

            // Row background alternate
            using var rowBgBrush = new SolidBrush(r % 2 == 0 ? ThemePalette.BackgroundDark : ThemePalette.BackgroundCard);
            g.FillRectangle(rowBgBrush, 0, y, totalWidth, RowHeight);
            g.DrawLine(borderPen, 0, y, totalWidth, y);

            // Room number badge
            g.DrawString($"№ {room.RoomNumber}", rowFont, textBrush, 10, y + 4);
            g.DrawString($"{room.Type} • {room.DailyRate:F0}₴", typeFont, mutedBrush, 10, y + 20);

            // Room status indicator
            Color statusColor = room.Status switch
            {
                RoomStatus.Available => ThemePalette.Success,
                RoomStatus.Occupied => ThemePalette.Danger,
                RoomStatus.Reserved => ThemePalette.Info,
                RoomStatus.Cleaning => ThemePalette.Warning,
                _ => ThemePalette.TextMuted
            };
            using var dotBrush = new SolidBrush(statusColor);
            g.FillEllipse(dotBrush, RoomColWidth - 16, y + 14, 8, 8);
        }

        // Bottom border
        g.DrawLine(borderPen, 0, totalHeight, totalWidth, totalHeight);

        // Draw Booking Blocks on top
        foreach (var b in _bookings)
        {
            int roomIndex = _rooms.FindIndex(r => r.Id == b.RoomId);
            if (roomIndex < 0) continue;

            int y = HeaderHeight + (roomIndex * RowHeight) + 4;
            int h = RowHeight - 8;

            int startDayOffset = (int)(b.CheckInDate.Date - _startDate.Date).TotalDays;
            int endDayOffset = (int)(b.CheckOutDate.Date - _startDate.Date).TotalDays;

            int x = RoomColWidth + (startDayOffset * DayColWidth) + 3;
            int width = Math.Max(25, (endDayOffset - startDayOffset) * DayColWidth - 6);

            if (x + width < RoomColWidth || x > totalWidth) continue;

            var blockRect = new Rectangle(x, y, width, h);
            using var blockPath = CreateRoundedRectangle(blockRect, 6);
            
            Color blockColor = b.IsCheckedIn ? Color.FromArgb(50, 65, 85) : Color.FromArgb(38, 46, 58);
            using var blockBrush = new SolidBrush(blockColor);
            g.FillPath(blockBrush, blockPath);

            using var blockBorder = new Pen(ThemePalette.Primary, 1.2f);
            g.DrawPath(blockBorder, blockPath);

            using var blockTextFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var whiteBrush = new SolidBrush(Color.White);
            string guestShort = b.GuestFullName.Length > 16 ? b.GuestFullName[..15] + "…" : b.GuestFullName;
            g.DrawString(guestShort, blockTextFont, whiteBrush, x + 6, y + 4);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        return path;
    }
}
