using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Controls;

public class DishCardControl : UserControl
{
    private readonly MenuItemDto _item;
    private bool _isHovered;

    public MenuItemDto ItemData => _item;
    public event EventHandler<MenuItemDto>? CardClicked;

    public DishCardControl(MenuItemDto item)
    {
        _item = item;
        Size = new Size(185, 120);
        Margin = new Padding(8);
        Cursor = Cursors.Hand;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
        DoubleBuffered = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        CardClicked?.Invoke(this, _item);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);

        // Draw card background
        using var path = CreateRoundedRectangle(rect, 8);
        using var bgBrush = new SolidBrush(_isHovered ? ThemePalette.BackgroundCardHover : ThemePalette.BackgroundCard);
        g.FillPath(bgBrush, path);

        using var borderPen = new Pen(_isHovered ? ThemePalette.Primary : ThemePalette.Border, 1.5f);
        g.DrawPath(borderPen, path);

        // Draw Category Tag (Auto-measured width)
        using var tagFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var tagBrush = new SolidBrush(ThemePalette.BackgroundInput);
        using var tagTextBrush = new SolidBrush(ThemePalette.TextMuted);
        var tagSize = g.MeasureString(_item.CategoryName, tagFont);
        int tagWidth = Math.Min((int)tagSize.Width + 10, Width - 90);
        var tagRect = new Rectangle(10, 8, tagWidth, 18);
        using var tagPath = CreateRoundedRectangle(tagRect, 4);
        g.FillPath(tagBrush, tagPath);
        
        using var sfTag = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(_item.CategoryName, tagFont, tagTextBrush, new RectangleF(13, 9, tagWidth - 6, 16), sfTag);

        // Prep time
        using var timeFont = new Font("Segoe UI", 8f, FontStyle.Regular);
        using var timeBrush = new SolidBrush(ThemePalette.TextMuted);
        string timeStr = $"⏱ {_item.PreparationMinutes} хв";
        g.DrawString(timeStr, timeFont, timeBrush, Width - 65, 10);

        // Dish Name (With Word Ellipsis)
        using var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(ThemePalette.TextPrimary);
        var titleRect = new RectangleF(10, 32, Width - 20, 48);
        using var sfTitle = new StringFormat { Trimming = StringTrimming.EllipsisWord };
        g.DrawString(_item.Name, titleFont, titleBrush, titleRect, sfTitle);

        // Price Badge
        string priceStr = $"{_item.Price:F0} ₴";
        using var priceFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var priceBrush = new SolidBrush(ThemePalette.Success);
        g.DrawString(priceStr, priceFont, priceBrush, 10, Height - 28);

        // Plus Icon button
        using var plusBrush = new SolidBrush(ThemePalette.BackgroundInput);
        g.FillEllipse(plusBrush, Width - 34, Height - 30, 24, 24);
        using var plusPen = new Pen(ThemePalette.BorderLight);
        g.DrawEllipse(plusPen, Width - 34, Height - 30, 24, 24);
        using var iconFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var iconBrush = new SolidBrush(ThemePalette.TextPrimary);
        g.DrawString("+", iconFont, iconBrush, Width - 28, Height - 32);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        return path;
    }
}
