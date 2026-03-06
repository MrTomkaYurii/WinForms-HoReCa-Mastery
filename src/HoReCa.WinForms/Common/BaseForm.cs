using System.Drawing;
using System.Windows.Forms;

namespace HoReCa.WinForms.Common;

public class BaseForm : Form
{
    public BaseForm()
    {
        // Flicker-free GDI+ rendering
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        DoubleBuffered = true;
        BackColor = ThemePalette.BackgroundDark;
        ForeColor = ThemePalette.TextPrimary;
        Font = ThemePalette.BodyFont;
        StartPosition = FormStartPosition.CenterScreen;
    }
}
