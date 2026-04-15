using System.Drawing;
using System.Windows.Forms;
using HoReCa.WinForms.Common;
using HoReCa.WinForms.Diagnostics;

namespace HoReCa.WinForms.Forms;

public class LogsViewerForm : BaseForm
{
    private readonly RichTextBox _rtbLogs;

    public LogsViewerForm()
    {
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(12)
        };

        var lblTitle = new Label
        {
            Text = "СИСТЕМНИЙ ЖУРНАЛ ПОДІЙ & ДІАГНОСТИКА (SERILOG REAL-TIME SINK)",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.Primary,
            Location = new Point(14, 16),
            AutoSize = true
        };

        var btnClear = new Button
        {
            Text = "🗑️ Очистити лог",
            Location = new Point(Width - 170, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(130, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnClear.FlatAppearance.BorderColor = ThemePalette.Border;
        btnClear.Click += (s, e) => _rtbLogs.Clear();

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, btnClear });

        _rtbLogs = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(10, 14, 23),
            ForeColor = Color.WhiteSmoke,
            Font = ThemePalette.MonospaceFont,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(10)
        };

        Controls.Add(_rtbLogs);
        Controls.Add(pnlHeader);

        SerilogRichTextBoxSink.SetTarget(_rtbLogs);
    }
}
