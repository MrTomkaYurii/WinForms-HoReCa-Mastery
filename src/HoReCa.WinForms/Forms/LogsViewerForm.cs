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
            Height = 56,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(16, 10, 16, 10)
        };

        var lblTitle = new Label
        {
            Text = "СИСТЕМНИЙ ЖУРНАЛ ПОДІЙ & ДІАГНОСТИКА (SERILOG REAL-TIME SINK)",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.Primary,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        var flpActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnClear = new Button
        {
            Text = "🗑️ Очистити лог",
            Size = new Size(150, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnClear.FlatAppearance.BorderColor = ThemePalette.Border;
        btnClear.Click += (s, e) => _rtbLogs.Clear();

        flpActions.Controls.Add(btnClear);

        pnlHeader.Controls.Add(flpActions);
        pnlHeader.Controls.Add(lblTitle);

        _rtbLogs = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.MonospaceFont,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(12)
        };

        Controls.Add(_rtbLogs);
        Controls.Add(pnlHeader);

        SerilogRichTextBoxSink.SetTarget(_rtbLogs);
    }
}
