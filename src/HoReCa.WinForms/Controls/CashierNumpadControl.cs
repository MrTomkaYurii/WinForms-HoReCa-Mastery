using System.Drawing;
using System.Windows.Forms;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Controls;

public class CashierNumpadControl : UserControl
{
    public event EventHandler<string>? DigitPressed;
    public event EventHandler? ClearPressed;
    public event EventHandler? BackspacePressed;
    public event EventHandler<decimal>? QuickCashPressed;

    public CashierNumpadControl()
    {
        Size = new Size(260, 310);
        BackColor = ThemePalette.BackgroundCard;
        Padding = new Padding(6);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 5,
            BackColor = Color.Transparent
        };

        for (int i = 0; i < 4; i++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        for (int i = 0; i < 5; i++) table.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

        string[,] keys =
        {
            { "7", "8", "9", "100" },
            { "4", "5", "6", "200" },
            { "1", "2", "3", "500" },
            { "0", "00", ".", "1000" },
            { "C", "⌫", "OK", "Точно" }
        };

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                string text = keys[r, c];
                var btn = CreateNumpadButton(text);

                btn.Click += (s, e) =>
                {
                    if (text == "C") ClearPressed?.Invoke(this, EventArgs.Empty);
                    else if (text == "⌫") BackspacePressed?.Invoke(this, EventArgs.Empty);
                    else if (text is "100" or "200" or "500" or "1000") QuickCashPressed?.Invoke(this, decimal.Parse(text));
                    else if (text != "OK" && text != "Точно") DigitPressed?.Invoke(this, text);
                };

                table.Controls.Add(btn, c, r);
            }
        }

        Controls.Add(table);
    }

    private static Button CreateNumpadButton(string text)
    {
        bool isSpecial = text is "C" or "⌫" or "OK" or "Точно";
        bool isQuickBill = text is "100" or "200" or "500" or "1000";

        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", isSpecial || isQuickBill ? 9.5f : 12f, FontStyle.Bold),
            ForeColor = isSpecial ? ThemePalette.Warning : (isQuickBill ? ThemePalette.Success : ThemePalette.TextPrimary),
            BackColor = isSpecial ? Color.FromArgb(36, 40, 48) : ThemePalette.BackgroundInput,
            Margin = new Padding(3),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = ThemePalette.Border;
        return btn;
    }
}
