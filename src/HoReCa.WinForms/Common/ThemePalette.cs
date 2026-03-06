using System.Drawing;

namespace HoReCa.WinForms.Common;

public static class ThemePalette
{
    // Backgrounds
    public static readonly Color BackgroundDark = Color.FromArgb(18, 24, 38);       // Deep slate navy
    public static readonly Color BackgroundCard = Color.FromArgb(28, 36, 54);       // Card surface
    public static readonly Color BackgroundInput = Color.FromArgb(36, 46, 68);      // Input fields
    public static readonly Color BackgroundSidebar = Color.FromArgb(14, 19, 31);    // Left sidebar

    // Accent colors
    public static readonly Color Primary = Color.FromArgb(56, 189, 248);            // Sky cyan
    public static readonly Color PrimaryDark = Color.FromArgb(14, 116, 144);        // Cyan active
    public static readonly Color Success = Color.FromArgb(52, 211, 153);            // Emerald
    public static readonly Color Warning = Color.FromArgb(251, 191, 36);            // Amber
    public static readonly Color Danger = Color.FromArgb(248, 113, 113);            // Ruby
    public static readonly Color Info = Color.FromArgb(129, 140, 248);              // Indigo

    // Text colors
    public static readonly Color TextPrimary = Color.FromArgb(241, 245, 249);      // Slate 100
    public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);        // Slate 400
    public static readonly Color TextInverse = Color.FromArgb(15, 23, 42);          // Dark text

    // Borders
    public static readonly Color Border = Color.FromArgb(51, 65, 85);               // Slate 700
    public static readonly Color BorderLight = Color.FromArgb(71, 85, 105);         // Slate 600

    // Fonts
    public static readonly Font HeaderFont = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font TitleFont = new("Segoe UI", 11f, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font SmallFont = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font MonospaceFont = new("Cascadia Code", 9.5f, FontStyle.Regular);
}
