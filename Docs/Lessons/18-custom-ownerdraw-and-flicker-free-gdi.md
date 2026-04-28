# Lesson 18: Custom OwnerDraw & Flicker-Free GDI+ Rendering

## 1. Overview

Default Windows Forms controls often suffer from noticeable flickering during resizing or rapid redrawing. This happens because Windows erases the background with `WM_ERASEBKGND` before redrawing the foreground with `WM_PAINT`. In this lesson, we eliminate flicker using double buffering and advanced GDI+ rendering.

---

## 2. Double Buffering & Control Styles

To eradicate flickering, any custom control or form must set the following flags in its constructor:

```csharp
public class BaseCustomControl : Control
{
    public BaseCustomControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);

        DoubleBuffered = true;
        UpdateStyles();
    }
}
```

### Explanation of Styles

| Style | Purpose |
| :--- | :--- |
| `AllPaintingInWmPaint` | Ignores `WM_ERASEBKGND` messages, preventing the white flash before paint. |
| `UserPaint` | Tells Windows that the control paints itself rather than delegating to the OS. |
| `OptimizedDoubleBuffer` | Renders all graphics into an offscreen memory buffer before blitting to the display. |
| `ResizeRedraw` | Automatically triggers a full repaint whenever width or height changes. |

---

## 3. High-Fidelity GDI+ Settings

When implementing `OnPaint(PaintEventArgs e)`:

```csharp
protected override void OnPaint(PaintEventArgs e)
{
    var g = e.Graphics;

    // Set GDI+ Quality Modes
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

    // Paint custom visual elements
    RenderControl(g);
}
```

---

## 4. Double Buffering DataGridView via Reflection

`DataGridView` has a protected `DoubleBuffered` property that is `false` by default. We can enable it via an extension method:

```csharp
public static class WinFormsExtensions
{
    public static void EnableDoubleBuffering(this DataGridView dgv)
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(dgv, true, null);
    }
}
```
