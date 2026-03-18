using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using HoReCa.WinForms.Controls;

namespace HoReCa.WinForms.Forms;

public class PaymentDialogForm : Form
{
    private readonly OrderDto _order;
    private readonly IReceiptPrintService _printService;
    private readonly Label _lblChange;
    private readonly TextBox _txtReceived;
    private readonly RadioButton _rbCash;
    private readonly RadioButton _rbCard;
    private readonly RadioButton _rbRoom;

    public PaymentMethod SelectedPaymentMethod
    {
        get
        {
            if (_rbCard.Checked) return PaymentMethod.CreditCard;
            if (_rbRoom.Checked) return PaymentMethod.RoomCharge;
            return PaymentMethod.Cash;
        }
    }

    public PaymentDialogForm(OrderDto order, IReceiptPrintService printService)
    {
        _order = order;
        _printService = printService;

        Text = $"Оплата рахунку: {order.OrderNumber}";
        Size = new Size(580, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ThemePalette.BackgroundDark;
        ForeColor = ThemePalette.TextPrimary;
        Font = ThemePalette.BodyFont;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(16, 12, 16, 0)
        };

        var lblTotalDue = new Label
        {
            Text = $"СУМА ДО СПЛАТИ: {_order.TotalAmount:F2} ₴",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ThemePalette.Success,
            Dock = DockStyle.Top
        };
        pnlHeader.Controls.Add(lblTotalDue);

        // Methods Radio buttons
        var gbMethods = new GroupBox
        {
            Text = "Метод оплати",
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(20, 80),
            Size = new Size(240, 130)
        };

        _rbCash = new RadioButton { Text = "💵 Готівка (Cash)", Checked = true, Location = new Point(16, 25), AutoSize = true, ForeColor = ThemePalette.TextPrimary };
        _rbCard = new RadioButton { Text = "💳 Картка / POS", Location = new Point(16, 55), AutoSize = true, ForeColor = ThemePalette.TextPrimary };
        _rbRoom = new RadioButton { Text = "🏨 На номер гостя", Location = new Point(16, 85), AutoSize = true, ForeColor = ThemePalette.TextPrimary };

        gbMethods.Controls.AddRange(new Control[] { _rbCash, _rbCard, _rbRoom });

        // Amount received & Change
        var pnlCashCalc = new Panel
        {
            Location = new Point(20, 220),
            Size = new Size(240, 140)
        };

        var lblRec = new Label { Text = "Отримано від клієнта (₴):", Location = new Point(0, 0), AutoSize = true };
        _txtReceived = new TextBox
        {
            Location = new Point(0, 24),
            Size = new Size(230, 32),
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.Primary,
            Text = $"{_order.TotalAmount:F0}"
        };
        _txtReceived.TextChanged += (s, e) => RecalculateChange();

        _lblChange = new Label
        {
            Text = "Решта: 0.00 ₴",
            Location = new Point(0, 68),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemePalette.Warning,
            AutoSize = true
        };

        pnlCashCalc.Controls.AddRange(new Control[] { lblRec, _txtReceived, _lblChange });

        // NumPad for touch input
        var numpad = new CashierNumpadControl
        {
            Location = new Point(280, 80)
        };
        numpad.DigitPressed += (s, key) => _txtReceived.Text += key;
        numpad.ClearPressed += (s, e) => _txtReceived.Text = "";
        numpad.BackspacePressed += (s, e) =>
        {
            if (_txtReceived.Text.Length > 0)
                _txtReceived.Text = _txtReceived.Text[..^1];
        };
        numpad.QuickCashPressed += (s, cash) => _txtReceived.Text = cash.ToString();

        // Bottom Actions
        var btnConfirm = new Button
        {
            Text = "ПІДТВЕРДИТИ ТА ЗАКРИТИ ЧЕК",
            DialogResult = DialogResult.OK,
            Location = new Point(20, 420),
            Size = new Size(340, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = ThemePalette.TitleFont,
            Cursor = Cursors.Hand
        };
        btnConfirm.FlatAppearance.BorderSize = 0;

        var btnPrint = new Button
        {
            Text = "🖨️ Чек",
            Location = new Point(370, 420),
            Size = new Size(90, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderColor = ThemePalette.Border;
        btnPrint.Click += (s, e) => _printService.PrintReceipt(_order, "LuxeResto HoReCa", "Юрій Т.", previewOnly: true);

        var btnCancel = new Button
        {
            Text = "Скасувати",
            DialogResult = DialogResult.Cancel,
            Location = new Point(470, 420),
            Size = new Size(80, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextMuted,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ThemePalette.Border;

        Controls.AddRange(new Control[] { pnlHeader, gbMethods, pnlCashCalc, numpad, btnConfirm, btnPrint, btnCancel });

        RecalculateChange();
    }

    private void RecalculateChange()
    {
        if (decimal.TryParse(_txtReceived.Text, out decimal received))
        {
            decimal change = received - _order.TotalAmount;
            _lblChange.Text = change >= 0 ? $"Решта: {change:F2} ₴" : $"Не вистачає: {Math.Abs(change):F2} ₴";
            _lblChange.ForeColor = change >= 0 ? ThemePalette.Success : ThemePalette.Danger;
        }
    }
}
