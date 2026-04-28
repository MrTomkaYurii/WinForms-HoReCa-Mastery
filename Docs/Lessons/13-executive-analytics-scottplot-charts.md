# Lesson 13: Executive Analytics & High-Performance Plotting with ScottPlot 5

## 1. Overview

Executives and hotel managers require visual KPI insights: daily revenue breakdown, average check sizes, room occupancy rates, and sales trends. In modern .NET desktop applications, **ScottPlot 5** provides high-performance, interactive, and customizable charts without heavyweight commercial licensing.

---

## 2. Setting Up ScottPlot in WinForms

Install the ScottPlot WinForms package:
```xml
<PackageReference Include="ScottPlot.WinForms" Version="5.0.54" />
```

Add the `FormsPlot` control to the analytics form layout:

```csharp
private readonly ScottPlot.WinForms.FormsPlot _plotRevenue = new();

private void InitializeCharts()
{
    _plotRevenue.Dock = DockStyle.Fill;
    pnlChartContainer.Controls.Add(_plotRevenue);
}
```

---

## 3. Populating Bar Charts with Dark Theme Styling

ScottPlot 5 makes it straightforward to configure dark theme palettes matching the application's slate styling:

```csharp
public void Render7DayRevenueChart(AnalyticsSummaryDto data)
{
    var plot = _plotRevenue.Plot;
    plot.Clear();

    // Configure Dark Slate Palette
    plot.FigureBackground.Color = ScottPlot.Color.FromHex("#0F172A"); // BackgroundDark
    plot.DataBackground.Color = ScottPlot.Color.FromHex("#1E293B");   // SurfaceDark
    plot.Axes.Color(ScottPlot.Color.FromHex("#94A3B8"));              // TextMuted

    // Prepare Data
    var days = data.DailyRevenue.Keys.ToArray();
    var values = data.DailyRevenue.Values.Select(v => (double)v).ToArray();

    // Create Bars
    var bars = new List<ScottPlot.Bar>();
    for (int i = 0; i < values.Length; i++)
    {
        bars.Add(new ScottPlot.Bar
        {
            Position = i,
            Value = values[i],
            FillColor = ScottPlot.Color.FromHex("#10B981"), // Emerald Accent
            BorderColor = ScottPlot.Color.FromHex("#34D399"),
            BorderLineWidth = 1
        });
    }

    plot.Add.Bars(bars);

    // Configure Custom X-Axis Tick Labels
    var tickPositions = Enumerable.Range(0, days.Length).Select(i => (double)i).ToArray();
    plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickPositions, days);

    // Title and Labels
    plot.Title("Виручка за останні 7 днів (₴)");
    plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex("#F8FAFC");
    plot.Axes.Title.Label.FontSize = 14;

    _plotRevenue.Refresh();
}
```

---

## 4. Summary KPI Cards

Alongside charts, summary cards display key figures:
- **Total Revenue (Today)**: `24,850.00 ₴`
- **Hotel Occupancy**: `85.7%`
- **Average Bill (POS)**: `420.00 ₴`
- **Active Kitchen Tickets**: `3`

---

## 5. Key Takeaways

1. **Hardware Acceleration**: ScottPlot 5 supports SkiaSharp rendering under the hood, delivering 60 FPS panning, zooming, and resizing.
2. **`FormsPlot.Refresh()`**: Call `.Refresh()` on the UI thread once after updating data to minimize redundant paint passes.
