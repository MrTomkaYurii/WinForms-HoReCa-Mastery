using System.Drawing;

namespace HoReCa.WinForms.Common;

public static class ThemePalette
{
    // Backgrounds — Neutral Charcoal / Deep Slate (Eye-friendly, no harsh contrast)
    public static readonly Color BackgroundDark = Color.FromArgb(18, 18, 22);          // Deep graphite canvas
    public static readonly Color BackgroundCard = Color.FromArgb(28, 28, 34);          // Neutral card container
    public static readonly Color BackgroundCardHover = Color.FromArgb(38, 38, 46);     // Interactive hover state
    public static readonly Color BackgroundInput = Color.FromArgb(24, 24, 30);         // Inset input field
    public static readonly Color BackgroundSidebar = Color.FromArgb(13, 13, 16);       // Left navigation panel
    public static readonly Color BackgroundHeader = Color.FromArgb(22, 22, 26);        // Top header bar

    // Accents — Understated Steel / Platinum (Minimalist, professional)
    public static readonly Color Primary = Color.FromArgb(150, 175, 205);              // Muted steel slate
    public static readonly Color PrimaryDark = Color.FromArgb(55, 68, 88);              // Deep steel tone
    public static readonly Color AccentSubtle = Color.FromArgb(70, 80, 96);             // Secondary active tone

    // Status Colors — Strictly semantic (Used only when status indication is required)
    public static readonly Color Success = Color.FromArgb(68, 155, 110);               // Soft sage (Free table, normal stock)
    public static readonly Color Warning = Color.FromArgb(205, 150, 55);               // Warm ochre (Reserved, warning stock)
    public static readonly Color Danger = Color.FromArgb(205, 75, 75);                 // Brick red (Occupied, critical deficit)
    public static readonly Color Info = Color.FromArgb(100, 135, 180);                 // Slate blue (Info, booking)

    // Text hierarchy
    public static readonly Color TextPrimary = Color.FromArgb(232, 235, 240);          // Crisp soft white
    public static readonly Color TextMuted = Color.FromArgb(140, 145, 158);            // Neutral medium grey
    public static readonly Color TextInverse = Color.FromArgb(18, 18, 22);             // Dark text for bright badges

    // Borders & Dividers
    public static readonly Color Border = Color.FromArgb(42, 42, 50);                  // Subtle container border
    public static readonly Color BorderLight = Color.FromArgb(60, 60, 72);             // Highlighted separator

    // Typography
    public static readonly Font HeaderFont = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font TitleFont = new("Segoe UI", 10.5f, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font SmallFont = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font MonospaceFont = new("Cascadia Code", 9.5f, FontStyle.Regular);
}
