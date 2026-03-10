using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Controls;

public class FloorTableWidget : UserControl
{
    private TableDto _table;
    private bool _isHovered;
    private bool _isDragging;
    private Point _dragStartPoint;

    public TableDto TableData => _table;
    public event EventHandler<TableDto>? TableClicked;
    public event EventHandler<UpdateTablePositionRequest>? PositionChanged;

    public FloorTableWidget(TableDto table)
    {
        _table = table;
        
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor,
            true);

        DoubleBuffered = true;
        BackColor = Color.Transparent;
        UpdateFromDto(table);

        Cursor = Cursors.Hand;
    }

    public void UpdateFromDto(TableDto table)
    {
        _table = table;
        Location = new Point(table.PositionX, table.PositionY);
        Size = new Size(table.Width, table.Height);
        Invalidate();
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStartPoint = e.Location;
            BringToFront();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isDragging)
        {
            int deltaX = e.X - _dragStartPoint.X;
            int deltaY = e.Y - _dragStartPoint.Y;

            Left = Math.Max(0, Left + deltaX);
            Top = Math.Max(0, Top + deltaY);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isDragging)
        {
            _isDragging = false;
            PositionChanged?.Invoke(this, new UpdateTablePositionRequest(_table.Id, Left, Top));
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        TableClicked?.Invoke(this, _table);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(2, 2, Width - 5, Height - 5);

        // Status color mapping
        Color statusColor = _table.Status switch
        {
            TableStatus.Free => ThemePalette.Success,
            TableStatus.Occupied => ThemePalette.Danger,
            TableStatus.BillRequested => ThemePalette.Warning,
            TableStatus.Reserved => ThemePalette.Info,
            _ => ThemePalette.TextMuted
        };

        // Draw shadow
        using var shadowPath = GetTablePath(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), _table.Shape);
        using var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0));
        g.FillPath(shadowBrush, shadowPath);

        // Fill background
        using var tablePath = GetTablePath(rect, _table.Shape);
        using var bgBrush = new SolidBrush(_isHovered ? Color.FromArgb(42, 53, 78) : ThemePalette.BackgroundCard);
        g.FillPath(bgBrush, tablePath);

        // Draw border
        using var borderPen = new Pen(_isHovered ? ThemePalette.Primary : statusColor, 2f);
        g.DrawPath(borderPen, tablePath);

        // Table Number Header
        string title = $"№ {_table.TableNumber}";
        using var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var textBrush = new SolidBrush(ThemePalette.TextPrimary);
        var sizeTitle = g.MeasureString(title, fontTitle);
        g.DrawString(title, fontTitle, textBrush, (Width - sizeTitle.Width) / 2, 8);

        // Capacity and Status subtitle
        string subtitle = $"{_table.Capacity} місць";
        if (_table.ActiveOrderTotal.HasValue && _table.ActiveOrderTotal > 0)
        {
            subtitle = $"{_table.ActiveOrderTotal:F0} ₴";
        }

        using var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var subBrush = new SolidBrush(_table.Status == TableStatus.Occupied ? ThemePalette.Warning : ThemePalette.TextMuted);
        var sizeSub = g.MeasureString(subtitle, fontSub);
        g.DrawString(subtitle, fontSub, subBrush, (Width - sizeSub.Width) / 2, Height - 22);

        // Status indicator dot
        using var dotBrush = new SolidBrush(statusColor);
        g.FillEllipse(dotBrush, Width - 14, 6, 8, 8);
    }

    private static GraphicsPath GetTablePath(Rectangle bounds, TableShape shape)
    {
        var path = new GraphicsPath();
        if (shape == TableShape.Round)
        {
            path.AddEllipse(bounds);
        }
        else
        {
            int radius = 10;
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
            path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
            path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
        }
        return path;
    }
}
