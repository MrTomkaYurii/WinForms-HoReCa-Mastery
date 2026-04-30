using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using HoReCa.WinForms.Controls;

namespace HoReCa.WinForms.Forms;

public class PosTerminalForm : BaseForm
{
    private readonly ITablePosService _posService;
    private readonly IReceiptPrintService _printService;
    private readonly IPdfReportService _pdfReportService;
    private readonly IAuthService _authService;

    // View Switching: Hall vs Menu
    private readonly Panel _pnlTopBar;
    private readonly Button _btnModeHall;
    private readonly Button _btnModeMenu;
    private readonly Panel _pnlViewContainer;
    private readonly Panel _pnlHallView;
    private readonly Panel _pnlMenuView;

    // Hall View Controls
    private readonly Panel _pnlFloorCanvas;
    private readonly FlowLayoutPanel _pnlZoneTabs;
    private int? _currentZoneId;

    // Menu & Order View Controls
    private SplitContainer _splitMenu = null!;
    private readonly TextBox _txtSearchDish;
    private readonly FlowLayoutPanel _pnlCategoryTabs;
    private readonly FlowLayoutPanel _pnlDishGrid;
    private readonly DataGridView _gridTicket;
    private readonly Label _lblTicketTable;
    private readonly Label _lblTicketWaiter;
    private readonly Label _lblTicketSubtotal;
    private readonly Label _lblTicketTotal;
    private readonly Button _btnSendToKitchen;
    private readonly Button _btnPay;
    private readonly Button _btnPrintPdf;
    private readonly Button _btnClearTicket;

    private TableDto? _selectedTable;
    private OrderDto? _activeOrder;
    private readonly List<CreateOrderItemRequest> _currentDraftItems = new();
    private List<MenuItemDto> _allMenuItems = new();
    private int? _currentCategoryId;

    public PosTerminalForm(
        ITablePosService posService,
        IReceiptPrintService printService,
        IPdfReportService pdfReportService,
        IAuthService authService)
    {
        _posService = posService;
        _printService = printService;
        _pdfReportService = pdfReportService;
        _authService = authService;

        Padding = new Padding(12);
        BackColor = ThemePalette.BackgroundDark;

        // 1. Top Navigation Bar
        _pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(12, 8, 12, 8)
        };

        var pnlModes = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _btnModeHall = new Button
        {
            Text = "🗺️  СХЕМА ЗАЛІВ",
            Height = 34,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCardHover,
            ForeColor = ThemePalette.Primary,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        _btnModeHall.FlatAppearance.BorderColor = ThemePalette.Primary;
        _btnModeHall.Click += (s, e) => SwitchToHallView();

        _btnModeMenu = new Button
        {
            Text = "🍽️  ЗАМОВЛЕННЯ & МЕНЮ",
            Height = 34,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextMuted,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        _btnModeMenu.FlatAppearance.BorderColor = ThemePalette.Border;
        _btnModeMenu.Click += (s, e) => SwitchToMenuView();

        pnlModes.Controls.AddRange(new Control[] { _btnModeHall, _btnModeMenu });

        var pnlTopRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnQuickArrange = new Button
        {
            Text = "📐 Вирівняти столики сіткою",
            Height = 34,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand
        };
        btnQuickArrange.FlatAppearance.BorderColor = ThemePalette.Border;
        btnQuickArrange.Click += async (s, e) => await ArrangeTablesInGridAsync();

        pnlTopRight.Controls.Add(btnQuickArrange);

        _pnlTopBar.Controls.Add(pnlTopRight);
        _pnlTopBar.Controls.Add(pnlModes);

        // 2. View Container
        _pnlViewContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            Padding = new Padding(0, 10, 0, 0)
        };

        // --- SUBVIEW 1: HALL VIEW ---
        _pnlHallView = new Panel { Dock = DockStyle.Fill, BackColor = ThemePalette.BackgroundDark };

        var pnlHallToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(12, 6, 12, 6)
        };

        _pnlZoneTabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlHallToolbar.Controls.Add(_pnlZoneTabs);

        _pnlFloorCanvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            AutoScroll = true
        };

        _pnlHallView.Controls.Add(_pnlFloorCanvas);
        _pnlHallView.Controls.Add(pnlHallToolbar);

        // --- SUBVIEW 2: MENU & ACTIVE TICKET VIEW ---
        _pnlMenuView = new Panel { Dock = DockStyle.Fill, BackColor = ThemePalette.BackgroundDark, Visible = false };

        _splitMenu = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel2,
            SplitterWidth = 6,
            BackColor = ThemePalette.BackgroundDark
        };
        _splitMenu.SizeChanged += (s, e) =>
        {
            if (_splitMenu.Width > 600)
            {
                var target = Math.Max(350, _splitMenu.Width - 440);
                if (target < _splitMenu.Width - 250)
                {
                    _splitMenu.SplitterDistance = target;
                }
            }
        };

        // Left / Center: Catalog Browser
        var pnlCatalog = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark
        };

        var pnlSearchAndFilters = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(12, 10, 12, 10)
        };
        pnlSearchAndFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _txtSearchDish = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = ThemePalette.BodyFont,
            PlaceholderText = "🔍 Пошук страви за назвою або інгредієнтом...",
            Margin = new Padding(0, 0, 0, 8)
        };
        _txtSearchDish.TextChanged += (s, e) => FilterDishes();

        _pnlCategoryTabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        pnlSearchAndFilters.Controls.Add(_txtSearchDish, 0, 0);
        pnlSearchAndFilters.Controls.Add(_pnlCategoryTabs, 0, 1);

        _pnlDishGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        pnlCatalog.Controls.Add(_pnlDishGrid);
        pnlCatalog.Controls.Add(pnlSearchAndFilters);
        _splitMenu.Panel1.Controls.Add(pnlCatalog);

        // Right: Active Guest Check / Ticket
        var pnlTicket = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(12)
        };

        var pnlTicketHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemePalette.BackgroundInput,
            Padding = new Padding(12, 8, 12, 8)
        };

        _lblTicketTable = new Label
        {
            Text = "СТОЛИК: НЕ ОБРАНО",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Dock = DockStyle.Top,
            Height = 24
        };

        _lblTicketWaiter = new Label
        {
            Text = "Офіціант: —  |  Статус: Нове",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 20
        };

        pnlTicketHeader.Controls.AddRange(new Control[] { _lblTicketWaiter, _lblTicketTable });

        // Ticket Items Grid
        _gridTicket = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemePalette.BackgroundCard,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            GridColor = ThemePalette.Border,
            RowTemplate = { Height = 32 }
        };

        _gridTicket.ColumnHeadersDefaultCellStyle.BackColor = ThemePalette.BackgroundSidebar;
        _gridTicket.ColumnHeadersDefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _gridTicket.ColumnHeadersDefaultCellStyle.Font = ThemePalette.TitleFont;
        _gridTicket.ColumnHeadersHeight = 34;

        _gridTicket.DefaultCellStyle.BackColor = ThemePalette.BackgroundCard;
        _gridTicket.DefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _gridTicket.DefaultCellStyle.SelectionBackColor = ThemePalette.BackgroundInput;
        _gridTicket.DefaultCellStyle.SelectionForeColor = ThemePalette.Primary;
        _gridTicket.DefaultCellStyle.Font = ThemePalette.BodyFont;

        _gridTicket.Columns.Add("Name", "Страва");
        _gridTicket.Columns.Add("Qty", "К-сть");
        _gridTicket.Columns.Add("Price", "Ціна");
        _gridTicket.Columns.Add("Total", "Сума");

        _gridTicket.Columns[0].FillWeight = 46;
        _gridTicket.Columns[0].MinimumWidth = 135;
        _gridTicket.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _gridTicket.Columns[0].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;

        _gridTicket.Columns[1].FillWeight = 16;
        _gridTicket.Columns[1].MinimumWidth = 46;
        _gridTicket.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridTicket.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

        _gridTicket.Columns[2].FillWeight = 19;
        _gridTicket.Columns[2].MinimumWidth = 62;
        _gridTicket.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridTicket.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

        _gridTicket.Columns[3].FillWeight = 19;
        _gridTicket.Columns[3].MinimumWidth = 68;
        _gridTicket.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridTicket.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

        // Ticket Controls Toolbar (Quantity Adjustments)
        var pnlQtyControls = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(6, 4, 6, 4)
        };

        var btnAddQty = new Button
        {
            Text = "➕ +1",
            UseMnemonic = false,
            Height = 28,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
        btnAddQty.FlatAppearance.BorderColor = ThemePalette.Border;
        btnAddQty.Click += (s, e) => AdjustSelectedQuantity(1);

        var btnSubQty = new Button
        {
            Text = "➖ -1",
            UseMnemonic = false,
            Height = 28,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
        btnSubQty.FlatAppearance.BorderColor = ThemePalette.Border;
        btnSubQty.Click += (s, e) => AdjustSelectedQuantity(-1);

        var btnRemoveLine = new Button
        {
            Text = "🗑️ Видалити рядок",
            UseMnemonic = false,
            Height = 28,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand
        };
        btnRemoveLine.FlatAppearance.BorderColor = ThemePalette.Border;
        btnRemoveLine.Click += (s, e) => RemoveSelectedLine();

        pnlQtyControls.Controls.AddRange(new Control[] { btnAddQty, btnSubQty, btnRemoveLine });

        // Ticket Bottom Financials & Action Buttons
        var pnlTicketBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 220,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(12, 8, 12, 10)
        };

        _lblTicketSubtotal = new Label
        {
            Text = "Сума замовлення: 0.00 ₴",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblTicketTotal = new Label
        {
            Text = "РАЗОМ ДО СПЛАТИ: 0.00 ₴",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = ThemePalette.Success,
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var tlpActionsTop = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 4)
        };
        tlpActionsTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
        tlpActionsTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));

        _btnSendToKitchen = new Button
        {
            Text = "🚀 НА КУХНЮ (KDS)",
            UseMnemonic = false,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.PrimaryDark,
            ForeColor = Color.White,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 4, 0)
        };
        _btnSendToKitchen.FlatAppearance.BorderSize = 0;
        _btnSendToKitchen.Click += async (s, e) => await SendOrderToKitchenAsync();

        _btnPay = new Button
        {
            Text = "💳 ОПЛАТИТИ",
            UseMnemonic = false,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 0, 0)
        };
        _btnPay.FlatAppearance.BorderSize = 0;
        _btnPay.Click += async (s, e) => await OpenPaymentDialogAsync();

        tlpActionsTop.Controls.Add(_btnSendToKitchen, 0, 0);
        tlpActionsTop.Controls.Add(_btnPay, 1, 0);

        var tlpActionsBottom = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 4)
        };
        tlpActionsBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
        tlpActionsBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));

        _btnPrintPdf = new Button
        {
            Text = "🖨️ ДРУК ЧЕКУ (PDF)",
            UseMnemonic = false,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 4, 0)
        };
        _btnPrintPdf.FlatAppearance.BorderColor = ThemePalette.Border;
        _btnPrintPdf.Click += async (s, e) => await PrintPdfReceiptAsync();

        var btnBackToHall = new Button
        {
            Text = "⬅ ДО СХЕМИ СТОЛІВ",
            UseMnemonic = false,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 0, 0)
        };
        btnBackToHall.FlatAppearance.BorderColor = ThemePalette.Border;
        btnBackToHall.Click += (s, e) => SwitchToHallView();

        tlpActionsBottom.Controls.Add(_btnPrintPdf, 0, 0);
        tlpActionsBottom.Controls.Add(btnBackToHall, 1, 0);

        _btnClearTicket = new Button
        {
            Text = "🗑️ Очистити поточне замовлення",
            UseMnemonic = false,
            Dock = DockStyle.Top,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 0)
        };
        _btnClearTicket.FlatAppearance.BorderColor = ThemePalette.Border;
        _btnClearTicket.Click += (s, e) => ClearDraft();

        pnlTicketBottom.Controls.Add(_btnClearTicket);
        pnlTicketBottom.Controls.Add(tlpActionsBottom);
        pnlTicketBottom.Controls.Add(tlpActionsTop);
        pnlTicketBottom.Controls.Add(_lblTicketTotal);
        pnlTicketBottom.Controls.Add(_lblTicketSubtotal);

        pnlTicket.Controls.Add(_gridTicket);
        pnlTicket.Controls.Add(pnlQtyControls);
        pnlTicket.Controls.Add(pnlTicketBottom);
        pnlTicket.Controls.Add(pnlTicketHeader);
        _splitMenu.Panel2.Controls.Add(pnlTicket);

        _pnlMenuView.Controls.Add(_splitMenu);

        _pnlViewContainer.Controls.Add(_pnlMenuView);
        _pnlViewContainer.Controls.Add(_pnlHallView);

        Controls.Add(_pnlViewContainer);
        Controls.Add(_pnlTopBar);

        Load += async (s, e) => await InitializeAsync();
    }

    private void SwitchToHallView()
    {
        _pnlHallView.Visible = true;
        _pnlMenuView.Visible = false;

        _btnModeHall.BackColor = ThemePalette.BackgroundCardHover;
        _btnModeHall.ForeColor = ThemePalette.Primary;
        _btnModeHall.FlatAppearance.BorderColor = ThemePalette.Primary;

        _btnModeMenu.BackColor = ThemePalette.BackgroundCard;
        _btnModeMenu.ForeColor = ThemePalette.TextMuted;
        _btnModeMenu.FlatAppearance.BorderColor = ThemePalette.Border;
    }

    private void SwitchToMenuView()
    {
        _pnlHallView.Visible = false;
        _pnlMenuView.Visible = true;

        if (_splitMenu != null && _splitMenu.Width > 600)
        {
            _splitMenu.SplitterDistance = Math.Max(350, _splitMenu.Width - 440);
        }

        _btnModeMenu.BackColor = ThemePalette.BackgroundCardHover;
        _btnModeMenu.ForeColor = ThemePalette.Primary;
        _btnModeMenu.FlatAppearance.BorderColor = ThemePalette.Primary;

        _btnModeHall.BackColor = ThemePalette.BackgroundCard;
        _btnModeHall.ForeColor = ThemePalette.TextMuted;
        _btnModeHall.FlatAppearance.BorderColor = ThemePalette.Border;
    }

    private async Task InitializeAsync()
    {
        // 1. Zone Tabs
        var zones = await _posService.GetZonesAsync();
        _pnlZoneTabs.Controls.Clear();

        var btnAllZones = CreateFilterTabButton("Всі зали", async () =>
        {
            _currentZoneId = null;
            await LoadTablesAsync();
        }, isSelected: true);
        _pnlZoneTabs.Controls.Add(btnAllZones);

        foreach (var z in zones)
        {
            int zid = z.Id;
            var btnZone = CreateFilterTabButton(z.Name, async () =>
            {
                _currentZoneId = zid;
                await LoadTablesAsync();
            });
            _pnlZoneTabs.Controls.Add(btnZone);
        }

        await LoadTablesAsync();

        // 2. Categories
        var categories = await _posService.GetMenuCategoriesAsync();
        _pnlCategoryTabs.Controls.Clear();

        var btnAllCat = CreateCategoryButton("Всі позиції", () =>
        {
            _currentCategoryId = null;
            FilterDishes();
        }, isSelected: true);
        _pnlCategoryTabs.Controls.Add(btnAllCat);

        foreach (var cat in categories)
        {
            int cid = cat.Id;
            var btnCat = CreateCategoryButton(cat.Name, () =>
            {
                _currentCategoryId = cid;
                FilterDishes();
            });
            _pnlCategoryTabs.Controls.Add(btnCat);
        }

        _allMenuItems = await _posService.GetMenuItemsAsync(null);
        FilterDishes();
    }

    private Button CreateFilterTabButton(string text, Func<Task> onClick, bool isSelected = false)
    {
        var btn = new Button
        {
            Text = text,
            UseMnemonic = false,
            Height = 32,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = isSelected ? ThemePalette.BackgroundCardHover : ThemePalette.BackgroundCard,
            ForeColor = isSelected ? ThemePalette.TextPrimary : ThemePalette.TextMuted,
            Font = ThemePalette.SmallFont,
            Margin = new Padding(2),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = isSelected ? ThemePalette.Primary : ThemePalette.Border;

        btn.Click += async (s, e) =>
        {
            foreach (Control c in _pnlZoneTabs.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = ThemePalette.BackgroundCard;
                    b.ForeColor = ThemePalette.TextMuted;
                    b.FlatAppearance.BorderColor = ThemePalette.Border;
                }
            }
            btn.BackColor = ThemePalette.BackgroundCardHover;
            btn.ForeColor = ThemePalette.TextPrimary;
            btn.FlatAppearance.BorderColor = ThemePalette.Primary;
            await onClick();
        };

        return btn;
    }

    private Button CreateCategoryButton(string text, Action onClick, bool isSelected = false)
    {
        var btn = new Button
        {
            Text = text,
            UseMnemonic = false,
            Height = 32,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = isSelected ? ThemePalette.BackgroundCardHover : ThemePalette.BackgroundCard,
            ForeColor = isSelected ? ThemePalette.TextPrimary : ThemePalette.TextMuted,
            Font = ThemePalette.SmallFont,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 6),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = isSelected ? ThemePalette.Primary : ThemePalette.Border;

        btn.Click += (s, e) =>
        {
            foreach (Control c in _pnlCategoryTabs.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = ThemePalette.BackgroundCard;
                    b.ForeColor = ThemePalette.TextMuted;
                    b.FlatAppearance.BorderColor = ThemePalette.Border;
                }
            }
            btn.BackColor = ThemePalette.BackgroundCardHover;
            btn.ForeColor = ThemePalette.TextPrimary;
            btn.FlatAppearance.BorderColor = ThemePalette.Primary;
            onClick();
        };

        return btn;
    }

    private async Task LoadTablesAsync()
    {
        _pnlFloorCanvas.Controls.Clear();
        var tables = await _posService.GetTablesByZoneAsync(_currentZoneId);

        foreach (var table in tables)
        {
            var widget = new FloorTableWidget(table);
            widget.TableClicked += async (s, t) =>
            {
                await SelectTableAsync(t);
                SwitchToMenuView();
            };
            widget.PositionChanged += async (s, req) => await _posService.UpdateTablePositionAsync(req);
            _pnlFloorCanvas.Controls.Add(widget);
        }
    }

    private async Task ArrangeTablesInGridAsync()
    {
        var tables = await _posService.GetTablesByZoneAsync(_currentZoneId);
        _pnlFloorCanvas.Controls.Clear();

        int startX = 30;
        int startY = 24;
        int tableWidth = 120;
        int tableHeight = 90;
        int gapX = 24;
        int gapY = 24;

        int canvasWidth = Math.Max(500, _pnlFloorCanvas.ClientSize.Width > 0 ? _pnlFloorCanvas.ClientSize.Width : 750);
        int cols = Math.Max(2, (canvasWidth - 60) / (tableWidth + gapX));

        for (int i = 0; i < tables.Count; i++)
        {
            int r = i / cols;
            int c = i % cols;
            int newX = startX + c * (tableWidth + gapX);
            int newY = startY + r * (tableHeight + gapY);

            var updatedDto = tables[i] with { PositionX = newX, PositionY = newY, Width = tableWidth, Height = tableHeight };
            var widget = new FloorTableWidget(updatedDto);
            widget.TableClicked += async (s, t) =>
            {
                await SelectTableAsync(t);
                SwitchToMenuView();
            };
            widget.PositionChanged += async (s, req) => await _posService.UpdateTablePositionAsync(req);
            _pnlFloorCanvas.Controls.Add(widget);

            await _posService.UpdateTablePositionAsync(new UpdateTablePositionRequest(tables[i].Id, newX, newY));
        }
    }

    private void FilterDishes()
    {
        _pnlDishGrid.Controls.Clear();
        string query = _txtSearchDish.Text.Trim().ToLowerInvariant();

        var filtered = _allMenuItems.Where(m =>
        {
            if (_currentCategoryId.HasValue && m.CategoryId != _currentCategoryId.Value)
                return false;

            if (!string.IsNullOrEmpty(query))
            {
                bool matchesName = m.Name.ToLowerInvariant().Contains(query);
                bool matchesDesc = m.Description?.ToLowerInvariant().Contains(query) ?? false;
                if (!matchesName && !matchesDesc) return false;
            }

            return true;
        }).ToList();

        foreach (var item in filtered)
        {
            var card = new DishCardControl(item);
            card.CardClicked += (s, m) => AddDishToOrder(m);
            _pnlDishGrid.Controls.Add(card);
        }
    }

    private async Task SelectTableAsync(TableDto table)
    {
        _selectedTable = table;
        _lblTicketTable.Text = $"СТОЛИК № {table.TableNumber}  •  {table.ZoneName}";

        var currentStaff = _authService.CurrentUser?.FullName ?? "Максим Іванов";
        _lblTicketWaiter.Text = $"Офіціант: {currentStaff}  |  Місць: {table.Capacity}";

        _currentDraftItems.Clear();
        _activeOrder = await _posService.GetActiveOrderForTableAsync(table.Id);

        RefreshTicketGrid();
    }

    private void AddDishToOrder(MenuItemDto dish)
    {
        if (_selectedTable == null)
        {
            MessageBox.Show("Будь ласка, спершу оберіть столик на схемі залу!", "Вибір столика", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SwitchToHallView();
            return;
        }

        var existing = _currentDraftItems.FirstOrDefault(i => i.MenuItemId == dish.Id);
        if (existing != null)
        {
            _currentDraftItems.Remove(existing);
            _currentDraftItems.Add(existing with { Quantity = existing.Quantity + 1 });
        }
        else
        {
            _currentDraftItems.Add(new CreateOrderItemRequest(dish.Id, 1, null));
        }

        RefreshTicketGrid();
    }

    private void AdjustSelectedQuantity(int delta)
    {
        if (_gridTicket.CurrentRow == null) return;
        string dishName = _gridTicket.CurrentRow.Cells[0].Value?.ToString() ?? "";

        // Only allow adjusting draft items
        var menuItem = _allMenuItems.FirstOrDefault(m => m.Name == dishName);
        if (menuItem != null)
        {
            var draft = _currentDraftItems.FirstOrDefault(d => d.MenuItemId == menuItem.Id);
            if (draft != null)
            {
                int newQty = draft.Quantity + delta;
                _currentDraftItems.Remove(draft);
                if (newQty > 0)
                {
                    _currentDraftItems.Add(draft with { Quantity = newQty });
                }
                RefreshTicketGrid();
            }
        }
    }

    private void RemoveSelectedLine()
    {
        if (_gridTicket.CurrentRow == null) return;
        string dishName = _gridTicket.CurrentRow.Cells[0].Value?.ToString() ?? "";

        var menuItem = _allMenuItems.FirstOrDefault(m => m.Name == dishName);
        if (menuItem != null)
        {
            var draft = _currentDraftItems.FirstOrDefault(d => d.MenuItemId == menuItem.Id);
            if (draft != null)
            {
                _currentDraftItems.Remove(draft);
                RefreshTicketGrid();
            }
        }
    }

    private void RefreshTicketGrid()
    {
        _gridTicket.Rows.Clear();
        decimal total = 0;

        // 1. Existing Active Order Items
        if (_activeOrder != null)
        {
            foreach (var item in _activeOrder.Items)
            {
                _gridTicket.Rows.Add(item.ItemName, $"x{item.Quantity}", $"{item.UnitPrice:N0} ₴", $"{item.TotalPrice:N2} ₴");
                total += item.TotalPrice;
            }
        }

        // 2. Draft / Newly added items
        foreach (var draft in _currentDraftItems)
        {
            var dish = _allMenuItems.FirstOrDefault(m => m.Id == draft.MenuItemId);
            if (dish != null)
            {
                decimal lineTotal = draft.Quantity * dish.Price;
                int rowIndex = _gridTicket.Rows.Add(dish.Name, $"x{draft.Quantity} [нове]", $"{dish.Price:N0} ₴", $"{lineTotal:N2} ₴");
                _gridTicket.Rows[rowIndex].DefaultCellStyle.ForeColor = ThemePalette.Primary;
                total += lineTotal;
            }
        }

        _lblTicketSubtotal.Text = $"Сума без знижки: {total:N2} ₴";
        _lblTicketTotal.Text = $"РАЗОМ ДО СПЛАТИ: {total:N2} ₴";
    }

    private async Task SendOrderToKitchenAsync()
    {
        if (_selectedTable == null)
        {
            MessageBox.Show("Оберіть столик!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_currentDraftItems.Count == 0)
        {
            MessageBox.Show("Додайте страви з меню до чеку!", "Чек порожній", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var waiterName = _authService.CurrentUser?.FullName ?? "Офіціант";
            var req = new CreateOrderRequest(_selectedTable.Id, null, waiterName, _currentDraftItems.ToList());

            // Creates order AND automatically deducts stock via recipes!
            _activeOrder = await _posService.CreateOrderAsync(req);
            _currentDraftItems.Clear();

            await LoadTablesAsync();
            await SelectTableAsync(_selectedTable);

            MessageBox.Show(
                $"Замовлення {_activeOrder.OrderNumber} успішно відправлено на кухню!\nІнгредієнти автоматично списано зі складу за техкартами.",
                "Відправлено на KDS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка відправки замовлення: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenPaymentDialogAsync()
    {
        if (_activeOrder == null)
        {
            MessageBox.Show("Немає збереженого активного замовлення для оплати. Відправте замовлення на кухню перед розрахунком.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new PaymentDialogForm(_activeOrder, _printService);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            await _posService.CloseOrderAndPayAsync(_activeOrder.Id, dlg.SelectedPaymentMethod, _activeOrder.TotalAmount);
            _activeOrder = null;
            _currentDraftItems.Clear();

            await LoadTablesAsync();
            if (_selectedTable != null) await SelectTableAsync(_selectedTable);

            MessageBox.Show("Замовлення успішно оплачено та закрито! Стіл звільнено.", "Оплата успішна", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SwitchToHallView();
        }
    }

    private async Task PrintPdfReceiptAsync()
    {
        if (_activeOrder == null)
        {
            MessageBox.Show("Немає активного сформованого замовлення для друку фіскального чеку.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var cashier = _authService.CurrentUser?.FullName ?? "Офіціант";
            var filePath = await _pdfReportService.GenerateReceiptPdfAsync(_activeOrder, cashier);

            MessageBox.Show($"Фіскальний чек успішно згенеровано!\nШлях: {filePath}", "Чек згенеровано", MessageBoxButtons.OK, MessageBoxIcon.Information);

            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch { }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка створення PDF чеку: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearDraft()
    {
        _currentDraftItems.Clear();
        RefreshTicketGrid();
    }
}
