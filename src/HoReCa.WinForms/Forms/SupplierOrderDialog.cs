using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Forms;

public class SupplierOrderDialog : Form
{
    private readonly IProcurementService _procurementService;
    private readonly IPdfReportService _pdfReportService;
    private readonly IInventoryService _inventoryService;

    private readonly ComboBox _cboSuppliers;
    private readonly DataGridView _gridItems;
    private readonly Label _lblTotal;
    private readonly DateTimePicker _dtpDelivery;
    private readonly TextBox _txtNotes;
    private List<InventoryDto> _currentSupplierItems = new();
    private List<SupplierDto> _suppliers = new();
    private PurchaseOrderDto? _createdOrder;

    public SupplierOrderDialog(
        IProcurementService procurementService,
        IPdfReportService pdfReportService,
        IInventoryService inventoryService)
    {
        _procurementService = procurementService;
        _pdfReportService = pdfReportService;
        _inventoryService = inventoryService;

        Text = "🚚 Формування замовлення постачальнику (B2B Requisition & Stock Intake)";
        Size = new Size(880, 680);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ThemePalette.BackgroundDark;
        ForeColor = ThemePalette.TextPrimary;
        Font = ThemePalette.BodyFont;
        Padding = new Padding(16);

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(14, 10, 14, 10)
        };

        var lblTitle = new Label
        {
            Text = "🚚 СФОРМУВАТИ ЗАМОВЛЕННЯ ПОСТАЧАЛЬНИКУ & ПРИХІД ТОВАРУ",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 22
        };

        var pnlSupplierSelect = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var lblSelectSupplier = new Label
        {
            Text = "Оберіть постачальника:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0)
        };

        _cboSuppliers = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 360,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.BodyFont
        };
        _cboSuppliers.SelectedIndexChanged += async (s, e) => await OnSupplierChangedAsync();

        var btnAutoFill = new Button
        {
            Text = "⚡ Авто-розрахунок дефіциту",
            Height = 26,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.Primary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(12, 1, 0, 0)
        };
        btnAutoFill.FlatAppearance.BorderColor = ThemePalette.Primary;
        btnAutoFill.Click += (s, e) => AutoFillDeficit();

        pnlSupplierSelect.Controls.AddRange(new Control[] { lblSelectSupplier, _cboSuppliers, btnAutoFill });

        pnlHeader.Controls.Add(pnlSupplierSelect);
        pnlHeader.Controls.Add(lblTitle);

        // Items Grid
        _gridItems = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemePalette.BackgroundCard,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            GridColor = ThemePalette.Border,
            RowTemplate = { Height = 32 }
        };

        _gridItems.ColumnHeadersDefaultCellStyle.BackColor = ThemePalette.BackgroundSidebar;
        _gridItems.ColumnHeadersDefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _gridItems.ColumnHeadersDefaultCellStyle.Font = ThemePalette.TitleFont;
        _gridItems.ColumnHeadersHeight = 36;

        _gridItems.DefaultCellStyle.BackColor = ThemePalette.BackgroundCard;
        _gridItems.DefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _gridItems.DefaultCellStyle.SelectionBackColor = ThemePalette.BackgroundInput;
        _gridItems.DefaultCellStyle.SelectionForeColor = ThemePalette.Primary;

        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Товар / Інгредієнт", ReadOnly = true, FillWeight = 35 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "Од.", ReadOnly = true, FillWeight = 10 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "Залишок", ReadOnly = true, FillWeight = 14 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinStock", HeaderText = "Мін. поріг", ReadOnly = true, FillWeight = 14 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cost", HeaderText = "Ціна за од.", ReadOnly = true, FillWeight = 15 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderQty", HeaderText = "Замовити к-сть", ReadOnly = false, FillWeight = 18 });

        _gridItems.CellValueChanged += (s, e) => RecalculateTotal();

        // Bottom Configuration & Actions
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 130,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(14, 10, 14, 10)
        };

        var tlpParams = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 4,
            RowCount = 1
        };
        tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
        tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
        tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
        tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var lblDtp = new Label { Text = "Очікувана поставка:", Font = ThemePalette.SmallFont, ForeColor = ThemePalette.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _dtpDelivery = new DateTimePicker { Value = DateTime.Today.AddDays(2), Format = DateTimePickerFormat.Short, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, Dock = DockStyle.Fill };
        var lblNotes = new Label { Text = "Примітки:", Font = ThemePalette.SmallFont, ForeColor = ThemePalette.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        _txtNotes = new TextBox { Text = "Планове поповнення дефіцитних залишків складу", BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, Dock = DockStyle.Fill };

        tlpParams.Controls.Add(lblDtp, 0, 0);
        tlpParams.Controls.Add(_dtpDelivery, 1, 0);
        tlpParams.Controls.Add(lblNotes, 2, 0);
        tlpParams.Controls.Add(_txtNotes, 3, 0);

        _lblTotal = new Label
        {
            Text = "СУМА ДО СПЛАТИ: 0.00 ₴",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = ThemePalette.Success,
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var pnlActionButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnClose = new Button
        {
            Text = "Закрити",
            Height = 34,
            Width = 90,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.BodyFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0)
        };
        btnClose.FlatAppearance.BorderColor = ThemePalette.Border;
        btnClose.Click += (s, e) => Close();

        var btnReceiveStock = new Button
        {
            Text = "📥 Оприбуткувати партію на склад",
            Height = 34,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0)
        };
        btnReceiveStock.FlatAppearance.BorderSize = 0;
        btnReceiveStock.Click += async (s, e) => await ReceiveStockAsync();

        var btnGeneratePdf = new Button
        {
            Text = "📄 Згенерувати B2B PDF Замовлення",
            Height = 34,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.PrimaryDark,
            ForeColor = Color.White,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0)
        };
        btnGeneratePdf.FlatAppearance.BorderSize = 0;
        btnGeneratePdf.Click += async (s, e) => await GenerateOrderPdfAsync();

        pnlActionButtons.Controls.AddRange(new Control[] { btnClose, btnReceiveStock, btnGeneratePdf });

        pnlBottom.Controls.Add(pnlActionButtons);
        pnlBottom.Controls.Add(_lblTotal);
        pnlBottom.Controls.Add(tlpParams);

        // Grid Container
        var pnlGridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 10, 0, 10),
            BackColor = ThemePalette.BackgroundDark
        };
        pnlGridContainer.Controls.Add(_gridItems);

        Controls.Add(pnlGridContainer);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);

        Load += async (s, e) => await InitializeSuppliersAsync();
    }

    private async Task InitializeSuppliersAsync()
    {
        _suppliers = await _procurementService.GetSuppliersAsync();
        _cboSuppliers.Items.Clear();

        foreach (var s in _suppliers)
        {
            _cboSuppliers.Items.Add($"{s.Name} ({s.Category})");
        }

        if (_cboSuppliers.Items.Count > 0)
        {
            _cboSuppliers.SelectedIndex = 0;
        }
    }

    private async Task OnSupplierChangedAsync()
    {
        if (_cboSuppliers.SelectedIndex < 0) return;
        var selectedSupplier = _suppliers[_cboSuppliers.SelectedIndex];

        var allItems = await _inventoryService.GetAllInventoryAsync();
        _currentSupplierItems = allItems.Where(i => i.SupplierId == selectedSupplier.Id).ToList();

        // If no items tied specifically to this supplier, show related items or all items
        if (_currentSupplierItems.Count == 0)
        {
            _currentSupplierItems = allItems;
        }

        _gridItems.Rows.Clear();
        foreach (var itm in _currentSupplierItems)
        {
            decimal suggestedQty = itm.IsLowStock ? Math.Max(itm.MinimumStock * 2 - itm.CurrentStock, itm.MinimumStock) : 0m;
            int rowIndex = _gridItems.Rows.Add(
                itm.Id,
                itm.Name,
                itm.Unit,
                $"{itm.CurrentStock:F1}",
                $"{itm.MinimumStock:F1}",
                $"{itm.CostPerUnit:N2} ₴",
                suggestedQty > 0 ? $"{suggestedQty:F1}" : "0"
            );

            if (itm.IsLowStock)
            {
                _gridItems.Rows[rowIndex].Cells[3].Style.ForeColor = ThemePalette.Danger;
                _gridItems.Rows[rowIndex].Cells[3].Value = $"⚠️ {itm.CurrentStock:F1}";
            }
        }

        RecalculateTotal();
    }

    private void AutoFillDeficit()
    {
        foreach (DataGridViewRow row in _gridItems.Rows)
        {
            int itemId = (int)row.Cells[0].Value;
            var itm = _currentSupplierItems.FirstOrDefault(i => i.Id == itemId);
            if (itm != null && itm.IsLowStock)
            {
                decimal orderAmount = Math.Max(itm.MinimumStock * 2.5m - itm.CurrentStock, itm.MinimumStock);
                row.Cells[6].Value = $"{orderAmount:F1}";
            }
        }
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        decimal total = 0;
        foreach (DataGridViewRow row in _gridItems.Rows)
        {
            if (decimal.TryParse(row.Cells[6].Value?.ToString()?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal qty) && qty > 0)
            {
                int itemId = (int)row.Cells[0].Value;
                var itm = _currentSupplierItems.FirstOrDefault(i => i.Id == itemId);
                if (itm != null)
                {
                    total += qty * itm.CostPerUnit;
                }
            }
        }
        _lblTotal.Text = $"СУМА ДО СПЛАТИ: {total:N2} ₴";
    }

    private async Task<PurchaseOrderDto?> CreateOrderRecordAsync()
    {
        if (_cboSuppliers.SelectedIndex < 0) return null;
        var selectedSupplier = _suppliers[_cboSuppliers.SelectedIndex];

        var itemsToOrder = new List<CreatePurchaseOrderItemRequest>();
        foreach (DataGridViewRow row in _gridItems.Rows)
        {
            if (decimal.TryParse(row.Cells[6].Value?.ToString()?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal qty) && qty > 0)
            {
                int itemId = (int)row.Cells[0].Value;
                var itm = _currentSupplierItems.FirstOrDefault(i => i.Id == itemId);
                if (itm != null)
                {
                    itemsToOrder.Add(new CreatePurchaseOrderItemRequest(itemId, qty, itm.CostPerUnit));
                }
            }
        }

        if (itemsToOrder.Count == 0)
        {
            MessageBox.Show("Будь ласка, вкажіть кількість хоча б для однієї позиції товару!", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var req = new CreatePurchaseOrderRequest(
            selectedSupplier.Id,
            _dtpDelivery.Value,
            _txtNotes.Text.Trim(),
            itemsToOrder
        );

        _createdOrder = await _procurementService.CreatePurchaseOrderAsync(req);
        return _createdOrder;
    }

    private async Task GenerateOrderPdfAsync()
    {
        var order = await CreateOrderRecordAsync();
        if (order == null) return;

        try
        {
            var filePath = await _pdfReportService.GeneratePurchaseOrderPdfAsync(order);

            MessageBox.Show($"Документ {order.OrderNumber}.pdf успішно згенеровано!\nШлях: {filePath}", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);

            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch { }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка генерації PDF: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ReceiveStockAsync()
    {
        if (_createdOrder == null)
        {
            _createdOrder = await CreateOrderRecordAsync();
            if (_createdOrder == null) return;
        }

        var confirm = MessageBox.Show(
            $"Оприбуткувати партію {_createdOrder.OrderNumber} на суму {_createdOrder.TotalAmount:N2} ₴?\nЗалишки на складі буде автоматично збільшено.",
            "Підтвердження оприбуткування",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            await _procurementService.ReceivePurchaseOrderAsync(_createdOrder.Id);
            MessageBox.Show($"Партію {_createdOrder.OrderNumber} успішно оприбутковано на склад!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
