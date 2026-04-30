using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoReCa.WinForms.Forms;

public class MainShellForm : BaseForm
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;
    private readonly Panel _pnlContent;
    private readonly Label _lblClock;
    private readonly Label _lblUserBadge;
    private readonly ToolStripStatusLabel _lblDbStatus;
    private readonly ToolStripStatusLabel _lblActiveOrders;
    private readonly ToolStripStatusLabel _lblCurrentUser;
    private Button? _activeNavButton;

    // Navigation buttons
    private readonly Button _navStart;
    private readonly Button _navPos;
    private readonly Button _navHotel;
    private readonly Button _navKds;
    private readonly Button _navInventory;
    private readonly Button _navAnalytics;
    private readonly Button _navLogs;

    public MainShellForm(IServiceProvider serviceProvider, IAuthService authService)
    {
        Log.Information("MainShellForm constructor started.");
        _serviceProvider = serviceProvider;
        _authService = authService;

        Text = "LuxeResto HoReCa Suite — Enterprise POS & Hotel Management (.NET 10)";
        Size = new Size(1300, 800);
        MinimumSize = new Size(1000, 650);

        // Header Panel (Responsive Layout)
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = ThemePalette.BackgroundHeader,
            Padding = new Padding(16, 0, 16, 0)
        };

        var pnlHeaderLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 14, 0, 0)
        };

        var lblBrand = new Label
        {
            Text = "LUXERESTO  HoReCa  OS",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 0)
        };

        var lblSub = new Label
        {
            Text = "Enterprise ERP & Touch POS",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = ThemePalette.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 0)
        };

        pnlHeaderLeft.Controls.AddRange(new Control[] { lblBrand, lblSub });

        var pnlHeaderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 14, 14, 0)
        };

        var btnSwitchUser = new Button
        {
            Text = "🚪 Зміна акаунта",
            Size = new Size(135, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnSwitchUser.FlatAppearance.BorderColor = ThemePalette.Border;
        btnSwitchUser.Click += (s, e) => _navStart.PerformClick();

        _lblClock = new Label
        {
            Text = DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss"),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            AutoSize = true,
            Margin = new Padding(16, 6, 0, 0)
        };

        _lblUserBadge = new Label
        {
            Text = "👤 Оберіть акаунт на стартовій",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemePalette.Warning,
            AutoSize = true,
            Margin = new Padding(12, 6, 0, 0)
        };

        var clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        clockTimer.Tick += (s, e) => _lblClock.Text = DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss");
        clockTimer.Start();

        pnlHeaderRight.Controls.AddRange(new Control[] { btnSwitchUser, _lblClock, _lblUserBadge });

        pnlHeader.Controls.Add(pnlHeaderRight);
        pnlHeader.Controls.Add(pnlHeaderLeft);

        // Left Sidebar Navigation
        var pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(10)
        };

        _navStart = CreateNavButton("🏠  Старт & Акаунти", () => ShowStartView());
        _navPos = CreateNavButton("🍽️  Ресторан & POS", () => ShowView<PosTerminalForm>());
        _navHotel = CreateNavButton("🏨  Готель & Шахматка", () => ShowView<HotelFrontDeskForm>());
        _navKds = CreateNavButton("👨‍🍳  Кухня (KDS)", () => ShowView<KitchenDisplayForm>());
        _navInventory = CreateNavButton("📦  Склад & Товари", () => ShowView<InventoryForm>());
        _navAnalytics = CreateNavButton("📊  BI Аналітика", () => ShowView<AnalyticsForm>());
        _navLogs = CreateNavButton("📜  Журнал (Logs)", () => ShowView<LogsViewerForm>());

        pnlSidebar.Controls.Add(_navLogs);
        pnlSidebar.Controls.Add(_navAnalytics);
        pnlSidebar.Controls.Add(_navInventory);
        pnlSidebar.Controls.Add(_navKds);
        pnlSidebar.Controls.Add(_navHotel);
        pnlSidebar.Controls.Add(_navPos);
        pnlSidebar.Controls.Add(_navStart);

        // StatusStrip
        var statusStrip = new StatusStrip
        {
            BackColor = ThemePalette.BackgroundSidebar,
            ForeColor = ThemePalette.TextMuted,
            Font = ThemePalette.SmallFont
        };

        _lblDbStatus = new ToolStripStatusLabel("🟢 База даних: Підключено (PostgreSQL / SQLite)") { ForeColor = ThemePalette.Success };
        _lblCurrentUser = new ToolStripStatusLabel("  |  Користувач: Не обрано") { ForeColor = ThemePalette.Warning };
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
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Log.Information("MainShellForm.OnShown triggered. Calling _navStart.PerformClick()...");
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
        _navStart.PerformClick();
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

    public void ShowStartView()
    {
        Log.Information("MainShellForm.ShowStartView called.");
        _pnlContent.Controls.Clear();
        var startControl = _serviceProvider.GetRequiredService<WelcomeStartForm>();
        Log.Information("WelcomeStartForm resolved from DI.");
        startControl.Dock = DockStyle.Fill;
        startControl.UserLoggedIn += OnUserLoggedIn;
        _pnlContent.Controls.Add(startControl);
        Log.Information("WelcomeStartForm added to _pnlContent.");
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

    private void OnUserLoggedIn(UserAccountDto user)
    {
        _lblUserBadge.Text = $"👤 {user.FullName}  •  {user.Role}";
        _lblUserBadge.ForeColor = ThemePalette.Success;
        _lblCurrentUser.Text = $"  |  Користувач: {user.Username} ({user.Role})";
        _lblCurrentUser.ForeColor = ThemePalette.Primary;

        // Navigate to the role's primary workplace
        switch (user.Role)
        {
            case UserRole.Waiter:
                _navPos.PerformClick();
                break;
            case UserRole.Receptionist:
                _navHotel.PerformClick();
                break;
            case UserRole.Chef:
                _navKds.PerformClick();
                break;
            case UserRole.Storekeeper:
                _navInventory.PerformClick();
                break;
            case UserRole.Administrator:
                _navAnalytics.PerformClick();
                break;
            default:
                _navPos.PerformClick();
                break;
        }
    }
}
