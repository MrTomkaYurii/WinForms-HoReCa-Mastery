using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Controls;

public class KitchenTicketControl : UserControl
{
    private readonly OrderDto _order;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Label _lblTimer;
    private readonly Button _btnBump;

    public OrderDto OrderData => _order;
    public event EventHandler<int>? OrderBumped;

    public KitchenTicketControl(OrderDto order)
    {
        _order = order;
        Size = new Size(260, 320);
        Margin = new Padding(10);
        BackColor = ThemePalette.BackgroundCard;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
        DoubleBuffered = true;

        // Top Table & Order header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = ThemePalette.BackgroundSidebar
        };

        var lblOrderNum = new Label
        {
            Text = $"{order.OrderNumber} • Стіл #{order.TableNumber ?? 0}",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Location = new Point(10, 8),
            AutoSize = true
        };

        _lblTimer = new Label
        {
            Text = "00:00",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ThemePalette.Success,
            Location = new Point(190, 8),
            AutoSize = true
        };

        var lblWaiter = new Label
        {
            Text = $"Офіціант: {order.WaiterName}",
            Font = new Font("Segoe UI", 8f, FontStyle.Regular),
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(10, 28),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblOrderNum, _lblTimer, lblWaiter });

        // Items Flow Panel
        var pnlItems = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8)
        };

        foreach (var item in order.Items)
        {
            var pnlItemRow = new Panel { Size = new Size(230, 42), Margin = new Padding(0, 0, 0, 4) };
            var lblQtyName = new Label
            {
                Text = $"{item.Quantity}x  {item.ItemName}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ThemePalette.TextPrimary,
                Location = new Point(4, 2),
                AutoSize = true
            };
            var lblMod = new Label
            {
                Text = string.IsNullOrWhiteSpace(item.ModifiersOrNotes) ? "—" : item.ModifiersOrNotes,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = ThemePalette.Warning,
                Location = new Point(24, 22),
                AutoSize = true
            };
            pnlItemRow.Controls.AddRange(new Control[] { lblQtyName, lblMod });
            pnlItems.Controls.Add(pnlItemRow);
        }

        // Bottom Action Button
        _btnBump = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ThemePalette.TextInverse,
            Cursor = Cursors.Hand
        };
        _btnBump.FlatAppearance.BorderSize = 0;
        UpdateButtonState();

        _btnBump.Click += (s, e) => OrderBumped?.Invoke(this, _order.Id);

        Controls.Add(pnlItems);
        Controls.Add(pnlHeader);
        Controls.Add(_btnBump);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (s, e) => UpdateTimer();
        _timer.Start();
        UpdateTimer();
    }

    private void UpdateButtonState()
    {
        switch (_order.Status)
        {
            case OrderStatus.KitchenPending:
                _btnBump.Text = "ПРИЙНЯТИ В РОБОТУ";
                _btnBump.BackColor = ThemePalette.Primary;
                break;
            case OrderStatus.Cooking:
                _btnBump.Text = "СТРАВА ГОТОВА (READY)";
                _btnBump.BackColor = ThemePalette.Warning;
                break;
            case OrderStatus.Ready:
                _btnBump.Text = "ВИДАНО В ЗАЛ (BUMP)";
                _btnBump.BackColor = ThemePalette.Success;
                break;
            default:
                _btnBump.Text = "ЗАВЕРШИТИ";
                _btnBump.BackColor = ThemePalette.TextMuted;
                break;
        }
    }

    private void UpdateTimer()
    {
        var elapsed = DateTime.UtcNow - _order.CreatedAt;
        _lblTimer.Text = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";

        if (elapsed.TotalMinutes >= 20)
        {
            _lblTimer.ForeColor = ThemePalette.Danger;
        }
        else if (elapsed.TotalMinutes >= 10)
        {
            _lblTimer.ForeColor = ThemePalette.Warning;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
