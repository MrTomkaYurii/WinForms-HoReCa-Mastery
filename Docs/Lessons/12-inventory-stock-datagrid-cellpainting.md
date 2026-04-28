# Lesson 12: Inventory Management & Custom DataGridView CellPainting

## 1. Overview

Stock inventory requires clear visual status flags (e.g. "Low Stock", "In Stock", "Out of Stock"). While standard DataGridView displays raw strings or numbers, enterprise applications leverage the `CellPainting` event to draw custom badges, status pills, and warning icons directly into cells without third-party libraries.

---

## 2. The CellPainting Technique

By intercepting `CellPainting`, we can render custom GDI+ graphics into specific columns while letting default painting handle row backgrounds and selections:

```csharp
private void dgvInventory_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
{
    // Check if we are painting a content cell in the "Status" column
    if (e.RowIndex >= 0 && e.ColumnIndex == colStatus.Index)
    {
        e.PaintBackground(e.CellBounds, true);

        var item = (InventoryDto)dgvInventory.Rows[e.RowIndex].DataBoundItem;
        var isLowStock = item.IsLowStock;

        var badgeColor = isLowStock ? ThemePalette.RubyAccent : ThemePalette.PrimaryAccent;
        var badgeText = isLowStock ? "ДЕФІЦИТ" : "В НАЯВНОСТІ";

        var rect = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 4, e.CellBounds.Width - 16, e.CellBounds.Height - 8);

        // Draw rounded status badge
        using var brush = new SolidBrush(badgeColor);
        using var path = GetRoundedRectPath(rect, 4);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(brush, path);

        // Draw centered label
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(badgeText, font, Brushes.White, rect, sf);

        e.Handled = true; // Tell WinForms we completed the painting for this cell
    }
}
```

---

## 3. Helper for Rounded Rectangles in GDI+

```csharp
private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
{
    var path = new GraphicsPath();
    int d = radius * 2;
    path.AddArc(rect.X, rect.Y, d, d, 180, 90);
    path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
    path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
    path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
    path.CloseFigure();
    return path;
}
```

---

## 4. Key Takeaways

1. **`e.Handled = true`**: When you custom draw a cell, always set `e.Handled = true` so the default text rendering does not overdraw your graphics.
2. **Performance**: Do not allocate brushes or pens in loops without `using` statements; or keep cached pens/brushes to avoid GDI handle leaks during rapid scrolling.
3. **Double Buffering**: Always enable `DoubleBuffered` on the form and use reflection to set `DoubleBuffered = true` on the `DataGridView` to eradicate scrolling flicker.
