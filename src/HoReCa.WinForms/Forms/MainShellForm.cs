using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using Microsoft.Extensions.DependencyInjection;

namespace HoReCa.WinForms.Forms;

public class MainShellForm : BaseForm
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;
    private readonly Panel _pnlContent;
    private readonly Label _lblClock;
    private readonly ToolStripStatusLabel _lblDbStatus;
    private readonly ToolStripStatusLabel _lblActiveOrders;
    private readonly ToolStripStatusLabel _lblCurrentUser;
    private Button? _activeNavButton;

    public MainShellForm(IServiceProvider serviceProvider, IAuthService authService)
    {
        _serviceProvider = serviceProvider;
        _authService = authService;

        Text = "LuxeResto HoReCa Suite — Enterprise POS & Hotel Management (.NET 10)";
        Size = new Size(1300, 800);
        MinimumSize = new Size(1100, 700);

        var currentUser = _authService.CurrentUser;
        var roleBadge = currentUser != null ? $"{currentUser.FullName}  •  {currentUser.Role}" : "Демо Користувач";

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(20, 0, 20, 0)
        };

        var lblBrand = new Label
        {
            Text = "LUXERESTO  HoReCa  OS",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Location = new Point(20, 18),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Enterprise ERP & Touch POS",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(280, 24),
            AutoSize = true
        };

        var lblUserBadge = new Label
        {
            Text = $"👤 {roleBadge}",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemePalette.Success,
            Location = new Point(Width - 580, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true
        };

        _lblClock = new Label
        {
            Text = DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss"),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Location = new Point(Width - 270, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true
        };

        var btnLogout = new Button
        {
            Text = "🚪 Змінити акаунт",
            Location = new Point(Width - 110, 16),
            Size = new Size(95, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand
        };
        btnLogout.FlatAppearance.BorderColor = ThemePalette.Border;
        btnLogout.Click += (s, e) =>
        {
            _authService.Logout();
            DialogResult = DialogResult.Retry; // Signals to Program loop to re-show login
            Close();
        };

        var clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        clockTimer.Tick += (s, e) => _lblClock.Text = DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss");
        clockTimer.Start();

        pnlHeader.Controls.AddRange(new Control[] { lblBrand, lblSub, lblUserBadge, _lblClock, btnLogout });

        // Left Sidebar Navigation
        var pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(10)
        };

        var navPos = CreateNavButton("🍽️  Ресторан & POS", () => ShowView<PosTerminalForm>());
        var navHotel = CreateNavButton("🏨  Готель & Шахматка", () => ShowView<HotelFrontDeskForm>());
        var navKds = CreateNavButton("👨‍🍳  Кухня (KDS)", () => ShowView<KitchenDisplayForm>());
        var navInventory = CreateNavButton("📦  Склад & Товари", () => ShowView<InventoryForm>());
        var navAnalytics = CreateNavButton("📊  BI Аналітика", () => ShowView<AnalyticsForm>());
        var navLogs = CreateNavButton("📜  Журнал (Logs)", () => ShowView<LogsViewerForm>());

        pnlSidebar.Controls.Add(navLogs);
        pnlSidebar.Controls.Add(navAnalytics);
        pnlSidebar.Controls.Add(navInventory);
        pnlSidebar.Controls.Add(navKds);
        pnlSidebar.Controls.Add(navHotel);
        pnlSidebar.Controls.Add(navPos);

        // StatusStrip
        var statusStrip = new StatusStrip
        {
            BackColor = ThemePalette.BackgroundSidebar,
            ForeColor = ThemePalette.TextMuted,
            Font = ThemePalette.SmallFont
        };

        _lblDbStatus = new ToolStripStatusLabel("🟢 База даних: Підключено (PostgreSQL / SQLite)") { ForeColor = ThemePalette.Success };
        _lblCurrentUser = new ToolStripStatusLabel($"  |  Користувач: {currentUser?.Username ?? "demo"} ({currentUser?.Role})") { ForeColor = ThemePalette.Primary };
        _lblActiveOrders = new ToolStripStatusLabel("  |  Замовлень у роботі: Активно") { ForeColor = ThemePalette.TextPrimary };
        var lblVersion = new ToolStripStatusLabel("  |  v1.0.0-net10.0-LTS  |  WinForms Mastery Suite") { Alignment = ToolStripItemAlignment.Right };

        statusStrip.Items.AddRange(new ToolStripItem[] { _lblDbStatus, _lblCurrentUser, _lblActiveOrders, lblVersion });

        // Main Content Area
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark
        };

        Controls.Add(_pnlContent);
        Controls.Add(pnlSidebar);
        Controls.Add(pnlHeader);
        Controls.Add(statusStrip);

        // Role-based initial view
        if (currentUser != null)
        {
            switch (currentUser.Role)
            {
                case UserRole.Receptionist:
                    navHotel.PerformClick();
                    break;
                case UserRole.Chef:
                    navKds.PerformClick();
                    break;
                case UserRole.Storekeeper:
                    navInventory.PerformClick();
                    break;
                case UserRole.Administrator:
                    navAnalytics.PerformClick();
                    break;
                case UserRole.Waiter:
                default:
                    navPos.PerformClick();
                    break;
            }
        }
        else
        {
            navPos.PerformClick();
        }
    }

    private Button CreateNavButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ThemePalette.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Margin = new Padding(0, 0, 0, 6),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;

        btn.Click += (s, e) =>
        {
            if (_activeNavButton != null)
            {
                _activeNavButton.BackColor = Color.Transparent;
                _activeNavButton.ForeColor = ThemePalette.TextMuted;
            }
            _activeNavButton = btn;
            btn.BackColor = ThemePalette.BackgroundCard;
            btn.ForeColor = ThemePalette.Primary;
            onClick();
        };

        return btn;
    }

    public void ShowView<T>() where T : Form
    {
        _pnlContent.Controls.Clear();
        var form = _serviceProvider.GetRequiredService<T>();
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        _pnlContent.Controls.Add(form);
        form.Show();
    }
}
