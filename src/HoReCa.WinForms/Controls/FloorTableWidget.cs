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
            ControlStyles.OptimizedDoubleBuffer,
            true);

        DoubleBuffered = true;
        BackColor = ThemePalette.BackgroundDark;
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
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Ensure clean dark canvas background
        using var canvasBrush = new SolidBrush(ThemePalette.BackgroundDark);
        g.FillRectangle(canvasBrush, ClientRectangle);

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
        using var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        g.FillPath(shadowBrush, shadowPath);

        // Fill background
        using var tablePath = GetTablePath(rect, _table.Shape);
        using var bgBrush = new SolidBrush(_isHovered ? ThemePalette.BackgroundCardHover : ThemePalette.BackgroundCard);
        g.FillPath(bgBrush, tablePath);

        // Draw border
        using var borderPen = new Pen(_isHovered ? ThemePalette.Primary : statusColor, _isHovered ? 2.5f : 1.8f);
        g.DrawPath(borderPen, tablePath);

        // Inscribed safe area strictly inside geometry
        Rectangle safeRect;
        if (_table.Shape == TableShape.Round)
        {
            int insetX = (int)(Width * 0.16);
            int insetY = (int)(Height * 0.16);
            safeRect = new Rectangle(insetX, insetY, Width - 2 * insetX, Height - 2 * insetY);
        }
        else
        {
            safeRect = new Rectangle(10, 8, Width - 20, Height - 16);
        }

        using var sfCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        // Table Number Header
        string title = $"№ {_table.TableNumber}";
        using var fontTitle = new Font("Segoe UI", _table.Shape == TableShape.Round ? 11f : 12f, FontStyle.Bold);
        using var textBrush = new SolidBrush(ThemePalette.TextPrimary);
        int titleHeight = (int)(safeRect.Height * 0.38);
        var titleRect = new Rectangle(safeRect.X, safeRect.Y, safeRect.Width, titleHeight);
        g.DrawString(title, fontTitle, textBrush, titleRect, sfCenter);

        // Zone Name Tag (Ellipsized so it never spills out)
        string zoneStr = _table.ZoneName;
        using var fontZone = new Font("Segoe UI", 7.5f, FontStyle.Regular);
        using var zoneBrush = new SolidBrush(ThemePalette.TextMuted);
        int zoneHeight = (int)(safeRect.Height * 0.28);
        var zoneRect = new Rectangle(safeRect.X, safeRect.Y + titleHeight - 2, safeRect.Width, zoneHeight);
        g.DrawString(zoneStr, fontZone, zoneBrush, zoneRect, sfCenter);

        // Capacity and Status subtitle (Amount or Capacity)
        string subtitle = $"{_table.Capacity} місць";
        if (_table.ActiveOrderTotal.HasValue && _table.ActiveOrderTotal > 0)
        {
            subtitle = $"{_table.ActiveOrderTotal:N0} ₴";
        }

        using var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var subBrush = new SolidBrush(_table.Status == TableStatus.Occupied ? ThemePalette.Warning : (_table.Status == TableStatus.Free ? ThemePalette.Success : ThemePalette.TextMuted));
        int subHeight = (int)(safeRect.Height * 0.34);
        var subRect = new Rectangle(safeRect.X, safeRect.Bottom - subHeight, safeRect.Width, subHeight);
        g.DrawString(subtitle, fontSub, subBrush, subRect, sfCenter);

        // Status indicator dot (Placed safely inside border)
        using var dotBrush = new SolidBrush(statusColor);
        if (_table.Shape == TableShape.Round)
        {
            int dotX = (int)(Width * 0.74);
            int dotY = (int)(Height * 0.16);
            g.FillEllipse(dotBrush, dotX, dotY, 8, 8);
        }
        else
        {
            g.FillEllipse(dotBrush, Width - 18, 9, 8, 8);
        }
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
