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
    private readonly List<CreateOrderItemRequest> _currentDraftItems = new();
    
    private TableDto? _selectedTable;
    private OrderDto? _activeOrder;

    public PosTerminalForm(ITablePosService posService, IReceiptPrintService printService)
    {
        _posService = posService;
        _printService = printService;

        var splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 800,
            SplitterWidth = 4,
            BackColor = ThemePalette.Border
        };

        // LEFT: Floor Plan + Menu Items
        var splitLeft = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 380,
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
            Text = "ПЛАН ЗАЛУ (СХЕМА СТОЛИКІВ) — Перетягуйте столики для зміни плану",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.Primary,
            Location = new Point(12, 12),
            AutoSize = true
        };
        pnlFloorToolbar.Controls.Add(lblFloorTitle);

        var pnlFloorWrapper = new Panel { Dock = DockStyle.Fill };
        pnlFloorWrapper.Controls.Add(_pnlFloorCanvas);
        pnlFloorWrapper.Controls.Add(pnlFloorToolbar);
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
            Height = 65,
            BackColor = ThemePalette.BackgroundInput,
            Padding = new Padding(10)
        };

        _lblSelectedTable = new Label
        {
            Text = "СТОЛИК: НЕ ОБРАНО",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Location = new Point(10, 10),
            AutoSize = true
        };

        var lblWaiter = new Label
        {
            Text = "Офіціант: Юрій Т.  |  Зміна: Відкрита",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(10, 36),
            AutoSize = true
        };
        pnlOrderHeader.Controls.AddRange(new Control[] { _lblSelectedTable, lblWaiter });

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

        // Bottom payment & action panel
        var pnlOrderBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 160,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(10)
        };

        _lblOrderTotal = new Label
        {
            Text = "РАЗОМ: 0.00 ₴",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ThemePalette.Success,
            Location = new Point(10, 12),
            AutoSize = true
        };

        var btnSendToKitchen = new Button
        {
            Text = "ВІДПРАВИТИ НА КУХНЮ (KDS)",
            Location = new Point(10, 52),
            Size = new Size(200, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.PrimaryDark,
            ForeColor = Color.White,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand
        };
        btnSendToKitchen.FlatAppearance.BorderSize = 0;
        btnSendToKitchen.Click += async (s, e) => await SendOrderToKitchenAsync();

        var btnPay = new Button
        {
            Text = "ОПЛАТИТИ (PAY)",
            Location = new Point(220, 52),
            Size = new Size(160, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand
        };
        btnPay.FlatAppearance.BorderSize = 0;
        btnPay.Click += async (s, e) => await OpenPaymentDialogAsync();

        var btnPrintPrecheck = new Button
        {
            Text = "Друк пречека",
            Location = new Point(10, 102),
            Size = new Size(120, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnPrintPrecheck.FlatAppearance.BorderColor = ThemePalette.Border;
        btnPrintPrecheck.Click += (s, e) => PrintPrecheck();

        var btnClearOrder = new Button
        {
            Text = "Очистити",
            Location = new Point(140, 102),
            Size = new Size(90, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.Danger,
            Cursor = Cursors.Hand
        };
        btnClearOrder.FlatAppearance.BorderColor = ThemePalette.Border;
        btnClearOrder.Click += (s, e) => ClearDraft();

        pnlOrderBottom.Controls.AddRange(new Control[] { _lblOrderTotal, btnSendToKitchen, btnPay, btnPrintPrecheck, btnClearOrder });

        pnlOrderRight.Controls.Add(_gridCurrentOrder);
        pnlOrderRight.Controls.Add(pnlOrderBottom);
        pnlOrderRight.Controls.Add(pnlOrderHeader);
        splitMain.Panel2.Controls.Add(pnlOrderRight);

        Controls.Add(splitMain);

        Load += async (s, e) => await InitializeDataAsync(pnlCategoryFilter);
    }

    private async Task InitializeDataAsync(FlowLayoutPanel pnlCategoryFilter)
    {
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
        var tables = await _posService.GetTablesByZoneAsync();

        foreach (var table in tables)
        {
            var widget = new FloorTableWidget(table);
            widget.TableClicked += async (s, t) => await SelectTableAsync(t);
            widget.PositionChanged += async (s, req) => await _posService.UpdateTablePositionAsync(req);
            _pnlFloorCanvas.Controls.Add(widget);
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
