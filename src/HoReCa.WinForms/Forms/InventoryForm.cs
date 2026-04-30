using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Forms;

public class InventoryForm : BaseForm
{
    private readonly IInventoryService _inventoryService;
    private readonly DataGridView _grid;
    private readonly TextBox _txtSearch;
    private List<InventoryDto> _currentItems = new();

    public InventoryForm(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = ThemePalette.BackgroundHeader,
            Padding = new Padding(16, 0, 16, 0)
        };

        var lblTitle = new Label
        {
            Text = "СКЛАДСЬКИЙ ОБЛІК ІНГРЕДІЄНТІВ",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var pnlHeaderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };

        var btnAddStock = new Button
        {
            Text = "+ Поповнити залишок",
            Size = new Size(160, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnAddStock.FlatAppearance.BorderColor = ThemePalette.BorderLight;
        btnAddStock.Click += async (s, e) => await OpenAdjustStockDialogAsync();

        _txtSearch = new TextBox
        {
            Width = 240,
            Height = 30,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = ThemePalette.BodyFont,
            PlaceholderText = "🔍 Пошук за назвою або SKU..."
        };
        _txtSearch.TextChanged += async (s, e) => await LoadInventoryAsync();

        pnlHeaderRight.Controls.Add(btnAddStock);
        pnlHeaderRight.Controls.Add(_txtSearch);

        pnlHeader.Controls.Add(pnlHeaderRight);
        pnlHeader.Controls.Add(lblTitle);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemePalette.BackgroundDark,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            GridColor = ThemePalette.Border,
            RowTemplate = { Height = 34 }
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor = ThemePalette.BackgroundSidebar;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.Font = ThemePalette.TitleFont;
        _grid.DefaultCellStyle.BackColor = ThemePalette.BackgroundCard;
        _grid.DefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = ThemePalette.BackgroundCardHover;
        _grid.DefaultCellStyle.SelectionForeColor = ThemePalette.TextPrimary;

        _grid.Columns.Add("SKU", "Артикул (SKU)");
        _grid.Columns.Add("Name", "Назва інгредієнта");
        _grid.Columns.Add("Unit", "Од. вим.");
        _grid.Columns.Add("Stock", "Поточний залишок");
        _grid.Columns.Add("MinStock", "Мін. залишок");
        _grid.Columns.Add("Cost", "Собівартість");
        _grid.Columns.Add("Status", "Статус складу");

        _grid.Columns[0].FillWeight = 13; _grid.Columns[0].MinimumWidth = 85;
        _grid.Columns[1].FillWeight = 32; _grid.Columns[1].MinimumWidth = 160;
        _grid.Columns[2].FillWeight = 8;  _grid.Columns[2].MinimumWidth = 55;
        _grid.Columns[3].FillWeight = 11; _grid.Columns[3].MinimumWidth = 80;
        _grid.Columns[4].FillWeight = 11; _grid.Columns[4].MinimumWidth = 80;
        _grid.Columns[5].FillWeight = 10; _grid.Columns[5].MinimumWidth = 75;
        _grid.Columns[6].FillWeight = 15; _grid.Columns[6].MinimumWidth = 135;

        // Cell painting for low stock highlights (Never overflows boundaries)
        _grid.CellPainting += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 6)
            {
                e.PaintBackground(e.CellBounds, true);
                var val = e.Value?.ToString() ?? "";
                bool isLow = val.Contains("КРИТИЧНИЙ");

                Color badgeBg = isLow ? Color.FromArgb(45, ThemePalette.Danger) : Color.FromArgb(35, ThemePalette.Success);
                Color badgeBorder = isLow ? ThemePalette.Danger : ThemePalette.Success;
                Color badgeText = isLow ? ThemePalette.Danger : ThemePalette.Success;

                var rect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 4, e.CellBounds.Width - 12, e.CellBounds.Height - 8);
                using var bBrush = new SolidBrush(badgeBg);
                using var pPen = new Pen(badgeBorder, 1f);
                using var bFont = new Font("Segoe UI", 8f, FontStyle.Bold);

                e.Graphics.FillRectangle(bBrush, rect);
                e.Graphics.DrawRectangle(pPen, rect);
                TextRenderer.DrawText(e.Graphics, val, bFont, rect, badgeText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                e.Handled = true;
            }
        };

        Controls.Add(_grid);
        Controls.Add(pnlHeader);

        Load += async (s, e) => await LoadInventoryAsync();
    }

    private async Task LoadInventoryAsync()
    {
        _currentItems = await _inventoryService.GetAllInventoryAsync(_txtSearch.Text);
        _grid.Rows.Clear();

        foreach (var item in _currentItems)
        {
            string statusStr = item.IsLowStock ? "⚠️ КРИТИЧНИЙ ЗАЛИШОК" : "✓ В НОРМІ";
            _grid.Rows.Add(
                item.SKU,
                item.Name,
                item.Unit,
                $"{item.CurrentStock:F2}",
                $"{item.MinimumStock:F2}",
                $"{item.CostPerUnit:F2} ₴",
                statusStr
            );
        }
    }

    private async Task OpenAdjustStockDialogAsync()
    {
        if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _currentItems.Count)
        {
            MessageBox.Show("Будь ласка, виберіть позицію в таблиці для поповнення.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _currentItems[_grid.CurrentRow.Index];

        using var form = new Form
        {
            Text = $"Поповнення складу: {item.Name}",
            Size = new Size(380, 240),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label { Text = $"Кількість для додавання ({item.Unit}):", Location = new Point(20, 20), AutoSize = true };
        var num = new NumericUpDown
        {
            Location = new Point(20, 50),
            Size = new Size(320, 30),
            DecimalPlaces = 2,
            Minimum = 1,
            Maximum = 1000,
            Value = 10,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary
        };

        var btnOk = new Button
        {
            Text = "Підтвердити",
            DialogResult = DialogResult.OK,
            Location = new Point(200, 140),
            Size = new Size(140, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse
        };

        form.Controls.AddRange(new Control[] { lbl, num, btnOk });

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _inventoryService.AdjustStockAsync(item.Id, num.Value, "Ручне поповнення касиром");
            await LoadInventoryAsync();
            MessageBox.Show($"Залишок '{item.Name}' успішно поповнено на {num.Value} {item.Unit}!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
