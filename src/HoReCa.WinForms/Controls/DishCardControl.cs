using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Controls;

public class DishCardControl : UserControl
{
    private static readonly Dictionary<string, Image?> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly MenuItemDto _item;
    private bool _isHovered;

    public MenuItemDto ItemData => _item;
    public event EventHandler<MenuItemDto>? CardClicked;

    private static readonly ToolTip SharedToolTip = new() { InitialDelay = 600, AutoPopDelay = 4000, ReshowDelay = 300 };

    public DishCardControl(MenuItemDto item)
    {
        _item = item;
        Size = new Size(195, 220);
        Margin = new Padding(8);
        Cursor = Cursors.Hand;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
        DoubleBuffered = true;

        string tipText = $"{item.Name}\n" +
                         $"💰 Ціна: {item.Price:F0} ₴\n" +
                         (!string.IsNullOrWhiteSpace(item.Description) ? $"📝 {item.Description}\n" : "") +
                         $"⏱ Час приготування: {item.PreparationMinutes} хв\n\n(Клікніть для додавання в рахунок)";
        SharedToolTip.SetToolTip(this, tipText);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SharedToolTip.SetToolTip(this, null);
        }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        const int radius = 10;
        const int imgHeight = 115;

        // 1. Draw Card Background
        using var cardPath = CreateRoundedRectangle(rect, radius);
        using var bgBrush = new SolidBrush(_isHovered ? ThemePalette.BackgroundCardHover : ThemePalette.BackgroundCard);
        g.FillPath(bgBrush, cardPath);

        // 2. Image Banner (Top 115px)
        var imgRect = new Rectangle(1, 1, Width - 3, imgHeight);
        using var imgPath = CreateTopRoundedRectangle(imgRect, radius);

        string? effectiveImg = _item.ImageUrl;
        if (string.IsNullOrWhiteSpace(effectiveImg))
        {
            effectiveImg = $"Assets/Images/Dishes/dish_{Math.Max(1, (_item.Id % 42) == 0 ? 42 : (_item.Id % 42))}.jpg";
        }

        var img = GetDishImage(effectiveImg);
        if (img != null)
        {
            var oldClip = g.Clip;
            g.SetClip(imgPath, CombineMode.Replace);
            DrawImageAspectFill(g, img, imgRect);
            g.Clip = oldClip;
        }
        else
        {
            using var gradBrush = new LinearGradientBrush(imgRect, Color.FromArgb(45, 48, 55), Color.FromArgb(28, 30, 34), LinearGradientMode.ForwardDiagonal);
            g.FillPath(gradBrush, imgPath);

            string catEmoji = GetCategoryEmoji(_item.CategoryName);
            using var emojiFont = new Font("Segoe UI Emoji", 26f);
            using var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(catEmoji, emojiFont, Brushes.WhiteSmoke, imgRect, sfCenter);
        }

        // 3. Overlaid Badges on Image
        // Category Pill (Top-Left)
        using var tagFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var tagSize = g.MeasureString(_item.CategoryName, tagFont);
        int tagWidth = Math.Min((int)tagSize.Width + 12, Width - 80);
        var tagRect = new Rectangle(8, 8, tagWidth, 20);
        using var tagPath = CreateRoundedRectangle(tagRect, 5);
        using var pillBrush = new SolidBrush(Color.FromArgb(210, 18, 20, 24));
        g.FillPath(pillBrush, tagPath);
        using var sfTag = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center };
        g.DrawString(_item.CategoryName, tagFont, Brushes.White, new RectangleF(13, 8, tagWidth - 8, 20), sfTag);

        // Prep Time Pill (Top-Right)
        string timeStr = $"⏱ {_item.PreparationMinutes} хв";
        using var timeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var timeSize = g.MeasureString(timeStr, timeFont);
        int timeWidth = (int)timeSize.Width + 12;
        var timeRect = new Rectangle(Width - timeWidth - 8, 8, timeWidth, 20);
        using var timePath = CreateRoundedRectangle(timeRect, 5);
        g.FillPath(pillBrush, timePath);
        using var sfTime = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(timeStr, timeFont, Brushes.WhiteSmoke, timeRect, sfTime);

        // 4. Subtle divider under image
        using var divPen = new Pen(ThemePalette.Border, 1f);
        g.DrawLine(divPen, 1, imgHeight + 1, Width - 2, imgHeight + 1);

        // 5. Dish Title (Middle)
        using var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(ThemePalette.TextPrimary);
        var titleRect = new RectangleF(10, imgHeight + 8, Width - 20, 48);
        using var sfTitle = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.LineLimit };
        g.DrawString(_item.Name, titleFont, titleBrush, titleRect, sfTitle);

        // 6. Bottom Row: Price & Quick-Add Button
        string priceStr = $"{_item.Price:F0} ₴";
        using var priceFont = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        using var priceBrush = new SolidBrush(ThemePalette.Success);
        g.DrawString(priceStr, priceFont, priceBrush, 10, Height - 36);

        // Quick-Add Circle Button
        var btnAddRect = new Rectangle(Width - 36, Height - 36, 26, 26);
        using var btnAddBrush = new SolidBrush(_isHovered ? ThemePalette.Primary : ThemePalette.BackgroundInput);
        g.FillEllipse(btnAddBrush, btnAddRect);
        using var btnAddPen = new Pen(_isHovered ? ThemePalette.Primary : ThemePalette.BorderLight);
        g.DrawEllipse(btnAddPen, btnAddRect);

        using var plusFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var plusBrush = new SolidBrush(_isHovered ? Color.White : ThemePalette.TextPrimary);
        using var sfPlus = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("+", plusFont, plusBrush, new RectangleF(btnAddRect.X, btnAddRect.Y - 1, btnAddRect.Width, btnAddRect.Height), sfPlus);

        // 7. Outer Card Border
        using var borderPen = new Pen(_isHovered ? ThemePalette.Primary : ThemePalette.Border, _isHovered ? 2f : 1.2f);
        g.DrawPath(borderPen, cardPath);
    }

    private static void DrawImageAspectFill(Graphics g, Image img, Rectangle targetRect)
    {
        float imgAspect = (float)img.Width / img.Height;
        float targetAspect = (float)targetRect.Width / targetRect.Height;

        Rectangle srcRect;
        if (imgAspect > targetAspect)
        {
            int srcWidth = (int)(img.Height * targetAspect);
            int srcX = (img.Width - srcWidth) / 2;
            srcRect = new Rectangle(srcX, 0, srcWidth, img.Height);
        }
        else
        {
            int srcHeight = (int)(img.Width / targetAspect);
            int srcY = (img.Height - srcHeight) / 2;
            srcRect = new Rectangle(0, srcY, img.Width, srcHeight);
        }

        g.DrawImage(img, targetRect, srcRect, GraphicsUnit.Pixel);
    }

    private static Image? GetDishImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return null;

        if (_imageCache.TryGetValue(imagePath, out var cached))
            return cached;

        string[] possiblePaths =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "Dishes", Path.GetFileName(imagePath)),
            Path.Combine(Directory.GetCurrentDirectory(), imagePath),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Images", "Dishes", Path.GetFileName(imagePath)),
            imagePath
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    using var stream = new MemoryStream(File.ReadAllBytes(path));
                    var img = Image.FromStream(stream);
                    _imageCache[imagePath] = img;
                    return img;
                }
                catch
                {
                    // Ignore and try next
                }
            }
        }

        _imageCache[imagePath] = null;
        return null;
    }

    private static string GetCategoryEmoji(string category)
    {
        if (category.Contains("закуски", StringComparison.OrdinalIgnoreCase) || category.Contains("Антипасті", StringComparison.OrdinalIgnoreCase)) return "🥗";
        if (category.Contains("Супи", StringComparison.OrdinalIgnoreCase)) return "🍲";
        if (category.Contains("Гриль", StringComparison.OrdinalIgnoreCase) || category.Contains("Основні", StringComparison.OrdinalIgnoreCase)) return "🥩";
        if (category.Contains("піца", StringComparison.OrdinalIgnoreCase)) return "🍕";
        if (category.Contains("Паста", StringComparison.OrdinalIgnoreCase) || category.Contains("Різотто", StringComparison.OrdinalIgnoreCase)) return "🍝";
        if (category.Contains("Десерти", StringComparison.OrdinalIgnoreCase)) return "🍰";
        if (category.Contains("Кава", StringComparison.OrdinalIgnoreCase) || category.Contains("Чай", StringComparison.OrdinalIgnoreCase)) return "☕";
        if (category.Contains("Бар", StringComparison.OrdinalIgnoreCase) || category.Contains("Коктейлі", StringComparison.OrdinalIgnoreCase)) return "🍸";
        return "🍽️";
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

    private static GraphicsPath CreateTopRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
        path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
        path.CloseFigure();
        return path;
    }
}
