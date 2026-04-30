using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Entities;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using HoReCa.WinForms.Controls;

namespace HoReCa.WinForms.Forms;

public class PosTerminalForm : BaseForm
{
    private readonly ITablePosService _posService;
    private readonly IReceiptPrintService _printService;
    private readonly Panel _pnlFloorCanvas;
    private readonly FlowLayoutPanel _pnlMenuItems;
    private readonly DataGridView _gridCurrentOrder;
    private readonly Label _lblSelectedTable;
    private readonly Label _lblOrderTotal;
    private readonly FlowLayoutPanel _pnlZoneTabs;
    private readonly List<CreateOrderItemRequest> _currentDraftItems = new();
    
    private TableDto? _selectedTable;
    private OrderDto? _activeOrder;
    private int? _currentZoneId;

    public PosTerminalForm(ITablePosService posService, IReceiptPrintService printService)
    {
        _posService = posService;
        _printService = printService;

        var splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 620,
            SplitterWidth = 4,
            BackColor = ThemePalette.Border
        };

        Resize += (s, e) =>
        {
            if (ClientSize.Width > 800)
            {
                try
                {
                    int targetDist = Math.Max(350, ClientSize.Width - 360);
                    if (targetDist > 100 && targetDist < splitMain.Width - 100)
                    {
                        splitMain.SplitterDistance = targetDist;
                    }
                }
                catch { }
            }
        };

        // LEFT: Floor Plan + Menu Items
        var splitLeft = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 330,
            SplitterWidth = 4,
            BackColor = ThemePalette.Border
        };

        // Floor Plan Canvas
        _pnlFloorCanvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            AutoScroll = true
        };

        var pnlFloorToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(10, 6, 10, 6)
        };

        var lblFloorTitle = new Label
        {
            Text = "СХЕМА ЗАЛУ",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        _pnlZoneTabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        pnlFloorToolbar.Controls.Add(_pnlZoneTabs);
        pnlFloorToolbar.Controls.Add(lblFloorTitle);

        var pnlFloorWrapper = new Panel { Dock = DockStyle.Fill };
        pnlFloorWrapper.Controls.Add(_pnlFloorCanvas);
        pnlFloorWrapper.Controls.Add(pnlFloorToolbar);
        pnlFloorToolbar.BringToFront();
        splitLeft.Panel1.Controls.Add(pnlFloorWrapper);

        // Menu items selection panel
        var pnlMenuWrapper = new Panel { Dock = DockStyle.Fill, BackColor = ThemePalette.BackgroundDark };
        
        var pnlCategoryFilter = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(6),
            WrapContents = false,
            AutoScroll = true
        };

        _pnlMenuItems = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8),
            BackColor = ThemePalette.BackgroundDark
        };

        pnlMenuWrapper.Controls.Add(_pnlMenuItems);
        pnlMenuWrapper.Controls.Add(pnlCategoryFilter);
        splitLeft.Panel2.Controls.Add(pnlMenuWrapper);

        splitMain.Panel1.Controls.Add(splitLeft);

        // RIGHT: Active Check / Order Details
        var pnlOrderRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(12)
        };

        var pnlOrderHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = ThemePalette.BackgroundInput,
            Padding = new Padding(12, 8, 12, 8)
        };

        _lblSelectedTable = new Label
        {
            Text = "СТОЛИК: НЕ ОБРАНО",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Dock = DockStyle.Top,
            Height = 24,
            AutoEllipsis = true
        };

        var lblWaiter = new Label
        {
            Text = "Офіціант: Юрій Т.  |  Зміна: Відкрита",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 18,
            AutoEllipsis = true
        };
        pnlOrderHeader.Controls.AddRange(new Control[] { lblWaiter, _lblSelectedTable });

        // Order Grid
        _gridCurrentOrder = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemePalette.BackgroundCard,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            GridColor = ThemePalette.Border
        };

        _gridCurrentOrder.ColumnHeadersDefaultCellStyle.BackColor = ThemePalette.BackgroundSidebar;
        _gridCurrentOrder.ColumnHeadersDefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _gridCurrentOrder.ColumnHeadersDefaultCellStyle.Font = ThemePalette.TitleFont;
        _gridCurrentOrder.DefaultCellStyle.BackColor = ThemePalette.BackgroundCard;
        _gridCurrentOrder.DefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _gridCurrentOrder.DefaultCellStyle.SelectionBackColor = ThemePalette.BackgroundInput;
        _gridCurrentOrder.DefaultCellStyle.SelectionForeColor = ThemePalette.Primary;

        _gridCurrentOrder.Columns.Add("Name", "Страва");
        _gridCurrentOrder.Columns.Add("Qty", "К-сть");
        _gridCurrentOrder.Columns.Add("Price", "Ціна");
        _gridCurrentOrder.Columns.Add("Total", "Сума");

        _gridCurrentOrder.Columns[0].FillWeight = 50;
        _gridCurrentOrder.Columns[1].FillWeight = 15;
        _gridCurrentOrder.Columns[2].FillWeight = 17;
        _gridCurrentOrder.Columns[3].FillWeight = 18;

        // Bottom payment & action panel (Fully Responsive via TableLayoutPanels)
        var pnlOrderBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 168,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(10, 6, 10, 8)
        };

        _lblOrderTotal = new Label
        {
            Text = "РАЗОМ: 0.00 ₴",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ThemePalette.Success,
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var tlpActions1 = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 4)
        };
        tlpActions1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        tlpActions1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

        var btnSendToKitchen = new Button
        {
            Text = "🚀 НА КУХНЮ",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.PrimaryDark,
            ForeColor = Color.White,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 4, 0)
        };
        btnSendToKitchen.FlatAppearance.BorderSize = 0;
        btnSendToKitchen.Click += async (s, e) => await SendOrderToKitchenAsync();

        var btnPay = new Button
        {
            Text = "💳 ОПЛАТИТИ",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 0, 0)
        };
        btnPay.FlatAppearance.BorderSize = 0;
        btnPay.Click += async (s, e) => await OpenPaymentDialogAsync();

        tlpActions1.Controls.Add(btnSendToKitchen, 0, 0);
        tlpActions1.Controls.Add(btnPay, 1, 0);

        var tlpActions2 = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 0)
        };
        tlpActions2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        tlpActions2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

        var btnPrintPrecheck = new Button
        {
            Text = "🖨️ Пречек",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 4, 0)
        };
        btnPrintPrecheck.FlatAppearance.BorderColor = ThemePalette.Border;
        btnPrintPrecheck.Click += (s, e) => PrintPrecheck();

        var btnClearOrder = new Button
        {
            Text = "🗑️ Очистити",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 0, 0)
        };
        btnClearOrder.FlatAppearance.BorderColor = ThemePalette.Border;
        btnClearOrder.Click += (s, e) => ClearDraft();

        tlpActions2.Controls.Add(btnPrintPrecheck, 0, 0);
        tlpActions2.Controls.Add(btnClearOrder, 1, 0);

        pnlOrderBottom.Controls.Add(tlpActions2);
        pnlOrderBottom.Controls.Add(tlpActions1);
        pnlOrderBottom.Controls.Add(_lblOrderTotal);

        pnlOrderRight.Controls.Add(_gridCurrentOrder);
        pnlOrderRight.Controls.Add(pnlOrderBottom);
        pnlOrderRight.Controls.Add(pnlOrderHeader);
        splitMain.Panel2.Controls.Add(pnlOrderRight);

        Controls.Add(splitMain);

        Load += async (s, e) => await InitializeDataAsync(pnlCategoryFilter);
    }

    private async Task InitializeDataAsync(FlowLayoutPanel pnlCategoryFilter)
    {
        // Populate Zone Tabs
        var zones = await _posService.GetZonesAsync();
        _pnlZoneTabs.Controls.Clear();

        var btnAllZones = CreateFilterTabButton("Всі", async () =>
        {
            _currentZoneId = null;
            await LoadTablesAsync();
        }, isSelected: true);
        _pnlZoneTabs.Controls.Add(btnAllZones);

        foreach (var z in zones)
        {
            int zid = z.Id;
            string tabName = z.Name.Replace(" зал", "").Replace(" Лаундж", "").Trim();
            var btnZone = CreateFilterTabButton(tabName, async () =>
            {
                _currentZoneId = zid;
                await LoadTablesAsync();
            });
            _pnlZoneTabs.Controls.Add(btnZone);
        }

        var btnArrange = new Button
        {
            Text = "📐 Сітка",
            Height = 30,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Margin = new Padding(6, 2, 2, 2),
            Cursor = Cursors.Hand
        };
        btnArrange.FlatAppearance.BorderColor = ThemePalette.BorderLight;
        btnArrange.Click += async (s, e) => await ArrangeTablesInGridAsync();
        _pnlZoneTabs.Controls.Add(btnArrange);

        await LoadTablesAsync();

        // Load Categories
        var categories = await _posService.GetMenuCategoriesAsync();
        pnlCategoryFilter.Controls.Clear();

        var btnAll = CreateCategoryButton("Всі страви", async () => await LoadMenuItemsAsync(null));
        pnlCategoryFilter.Controls.Add(btnAll);

        foreach (var cat in categories)
        {
            var btn = CreateCategoryButton(cat.Name, async () => await LoadMenuItemsAsync(cat.Id));
            pnlCategoryFilter.Controls.Add(btn);
        }

        await LoadMenuItemsAsync(null);
    }

    private Button CreateFilterTabButton(string text, Func<Task> onClick, bool isSelected = false)
    {
        var btn = new Button
        {
            Text = text,
            Height = 30,
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
                if (c is Button b && b.Text != "📐 Вирівняти сіткою")
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

    private Button CreateCategoryButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Height = 32,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Margin = new Padding(3),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ThemePalette.Border;
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private async Task LoadTablesAsync()
    {
        _pnlFloorCanvas.Controls.Clear();
        var tables = await _posService.GetTablesByZoneAsync(_currentZoneId);

        // Check if any tables overlap; if so, organize them cleanly
        bool hasOverlap = CheckIfTablesOverlap(tables);
        if (hasOverlap)
        {
            await ArrangeTablesInGridAsync(tables);
            return;
        }

        foreach (var table in tables)
        {
            var widget = new FloorTableWidget(table);
            widget.TableClicked += async (s, t) => await SelectTableAsync(t);
            widget.PositionChanged += async (s, req) => await _posService.UpdateTablePositionAsync(req);
            _pnlFloorCanvas.Controls.Add(widget);
        }
    }

    private static bool CheckIfTablesOverlap(List<TableDto> tables)
    {
        for (int i = 0; i < tables.Count; i++)
        {
            for (int j = i + 1; j < tables.Count; j++)
            {
                var r1 = new Rectangle(tables[i].PositionX, tables[i].PositionY, tables[i].Width, tables[i].Height);
                var r2 = new Rectangle(tables[j].PositionX, tables[j].PositionY, tables[j].Width, tables[j].Height);
                if (r1.IntersectsWith(r2)) return true;
            }
        }
        return false;
    }

    private async Task ArrangeTablesInGridAsync(List<TableDto>? tablesToArrange = null)
    {
        var tables = tablesToArrange ?? await _posService.GetTablesByZoneAsync(_currentZoneId);
        _pnlFloorCanvas.Controls.Clear();

        int startX = 25;
        int startY = 20;
        int tableWidth = 125;
        int tableHeight = 90;
        int gapX = 24;
        int gapY = 24;

        int canvasWidth = Math.Max(500, _pnlFloorCanvas.ClientSize.Width > 0 ? _pnlFloorCanvas.ClientSize.Width : 750);
        int cols = Math.Max(2, (canvasWidth - 50) / (tableWidth + gapX));

        for (int i = 0; i < tables.Count; i++)
        {
            int r = i / cols;
            int c = i % cols;
            int newX = startX + c * (tableWidth + gapX);
            int newY = startY + r * (tableHeight + gapY);

            var updatedDto = tables[i] with { PositionX = newX, PositionY = newY, Width = tableWidth, Height = tableHeight };
            var widget = new FloorTableWidget(updatedDto);
            widget.TableClicked += async (s, t) => await SelectTableAsync(t);
            widget.PositionChanged += async (s, req) => await _posService.UpdateTablePositionAsync(req);
            _pnlFloorCanvas.Controls.Add(widget);

            // Persist the clean coordinates
            await _posService.UpdateTablePositionAsync(new UpdateTablePositionRequest(tables[i].Id, newX, newY));
        }
    }

    private async Task LoadMenuItemsAsync(int? categoryId)
    {
        _pnlMenuItems.Controls.Clear();
        var items = await _posService.GetMenuItemsAsync(categoryId);

        foreach (var item in items)
        {
            var card = new DishCardControl(item);
            card.CardClicked += (s, m) => AddItemToOrder(m);
            _pnlMenuItems.Controls.Add(card);
        }
    }

    private async Task SelectTableAsync(TableDto table)
    {
        _selectedTable = table;
        _lblSelectedTable.Text = $"СТОЛИК: № {table.TableNumber} ({table.ZoneName})";

        _currentDraftItems.Clear();
        _gridCurrentOrder.Rows.Clear();

        _activeOrder = await _posService.GetActiveOrderForTableAsync(table.Id);
        if (_activeOrder != null)
        {
            foreach (var item in _activeOrder.Items)
            {
                _gridCurrentOrder.Rows.Add(item.ItemName, item.Quantity, $"{item.UnitPrice:F0} ₴", $"{item.TotalPrice:F2} ₴");
            }
            _lblOrderTotal.Text = $"РАЗОМ: {_activeOrder.TotalAmount:F2} ₴";
        }
        else
        {
            _lblOrderTotal.Text = "РАЗОМ: 0.00 ₴";
        }
    }

    private void AddItemToOrder(MenuItemDto item)
    {
        if (_selectedTable == null)
        {
            MessageBox.Show("Будь ласка, спершу оберіть столик на схемі залу!", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var existing = _currentDraftItems.FirstOrDefault(i => i.MenuItemId == item.Id);
        if (existing != null)
        {
            _currentDraftItems.Remove(existing);
            _currentDraftItems.Add(existing with { Quantity = existing.Quantity + 1 });
        }
        else
        {
            _currentDraftItems.Add(new CreateOrderItemRequest(item.Id, 1, null));
        }

        RefreshGrid();
    }

    private void RefreshGrid()
    {
        _gridCurrentOrder.Rows.Clear();
        decimal total = 0;

        if (_activeOrder != null)
        {
            foreach (var itm in _activeOrder.Items)
            {
                _gridCurrentOrder.Rows.Add(itm.ItemName, itm.Quantity, $"{itm.UnitPrice:F0} ₴", $"{itm.TotalPrice:F2} ₴");
                total += itm.TotalPrice;
            }
        }

        // Add drafts
        foreach (var draft in _currentDraftItems)
        {
            _gridCurrentOrder.Rows.Add($"[НОВЕ] Item #{draft.MenuItemId}", draft.Quantity, "—", "—");
        }

        _lblOrderTotal.Text = $"РАЗОМ: {total:F2} ₴";
    }

    private async Task SendOrderToKitchenAsync()
    {
        if (_selectedTable == null || _currentDraftItems.Count == 0) return;

        var req = new CreateOrderRequest(_selectedTable.Id, null, "Офіціант", _currentDraftItems.ToList());
        _activeOrder = await _posService.CreateOrderAsync(req);
        _currentDraftItems.Clear();

        await LoadTablesAsync();
        await SelectTableAsync(_selectedTable);

        MessageBox.Show($"Замовлення {_activeOrder.OrderNumber} успішно відправлено на кухню (KDS)!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task OpenPaymentDialogAsync()
    {
        if (_activeOrder == null)
        {
            MessageBox.Show("Немає активного замовлення для оплати.", "Інфо", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        }
    }

    private void PrintPrecheck()
    {
        if (_activeOrder != null)
        {
            _printService.PrintReceipt(_activeOrder, "LuxeResto Restaurant", "Юрій Т.", previewOnly: true);
        }
    }

    private void ClearDraft()
    {
        _currentDraftItems.Clear();
        RefreshGrid();
    }
}
