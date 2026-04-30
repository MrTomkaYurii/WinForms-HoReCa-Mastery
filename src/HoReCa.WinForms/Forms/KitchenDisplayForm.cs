using System.Drawing;
using System.Media;
using System.Windows.Forms;
using HoReCa.Application.Interfaces;
using HoReCa.WinForms.Common;
using HoReCa.WinForms.Controls;

namespace HoReCa.WinForms.Forms;

public class KitchenDisplayForm : BaseForm
{
    private readonly IKitchenService _kitchenService;
    private readonly FlowLayoutPanel _pnlTickets;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private int _lastTicketsCount;

    public KitchenDisplayForm(IKitchenService kitchenService)
    {
        _kitchenService = kitchenService;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(16, 10, 16, 10)
        };

        var lblTitle = new Label
        {
            Text = "KDS — KITCHEN DISPLAY SYSTEM (КУХОННИЙ ТЕРМІНАЛ КУХАРЯ)",
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
            Size = new Size(120, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderColor = ThemePalette.Border;
        btnRefresh.Click += async (s, e) => await LoadKitchenOrdersAsync();

        flpActions.Controls.Add(btnRefresh);

        pnlHeader.Controls.Add(flpActions);
        pnlHeader.Controls.Add(lblTitle);

        _pnlTickets = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ThemePalette.BackgroundDark,
            Padding = new Padding(12)
        };

        Controls.Add(_pnlTickets);
        Controls.Add(pnlHeader);

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 10000 };
        _refreshTimer.Tick += async (s, e) => await LoadKitchenOrdersAsync();
        _refreshTimer.Start();

        Load += async (s, e) => await LoadKitchenOrdersAsync();
    }

    private async Task LoadKitchenOrdersAsync()
    {
        var orders = await _kitchenService.GetActiveKitchenOrdersAsync();

        if (orders.Count > _lastTicketsCount)
        {
            try { SystemSounds.Beep.Play(); } catch { }
        }
        _lastTicketsCount = orders.Count;

        _pnlTickets.Controls.Clear();

        if (orders.Count == 0)
        {
            var lblEmpty = new Label
            {
                Text = "Всі замовлення приготовані! На кухні зараз чисто й спокійно ☕",
                Font = ThemePalette.HeaderFont,
                ForeColor = ThemePalette.TextMuted,
                AutoSize = true,
                Margin = new Padding(40)
            };
            _pnlTickets.Controls.Add(lblEmpty);
            return;
        }

        foreach (var order in orders)
        {
            var ticket = new KitchenTicketControl(order);
            ticket.OrderBumped += async (s, orderId) =>
            {
                await _kitchenService.BumpOrderToNextStageAsync(orderId);
                await LoadKitchenOrdersAsync();
            };
            _pnlTickets.Controls.Add(ticket);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
