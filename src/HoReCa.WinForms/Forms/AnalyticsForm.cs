using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.WinForms.Common;
using ScottPlot.WinForms;

namespace HoReCa.WinForms.Forms;

public class AnalyticsForm : BaseForm
{
    private readonly IAnalyticsService _analyticsService;
    private readonly FormsPlot _plotRevenue;
    private readonly Label _lblTodayRevenue;
    private readonly Label _lblTodayOrders;
    private readonly Label _lblAverageCheck;
    private readonly Label _lblOccupancy;

    public AnalyticsForm(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;

        // Top KPI Cards Panel
        var pnlKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(12)
        };

        for (int i = 0; i < 4; i++) pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        _lblTodayRevenue = CreateKpiCard(pnlKpis, 0, "ВИРУЧКА ЗА СЬОГОДНІ", "0.00 ₴", ThemePalette.Success);
        _lblTodayOrders = CreateKpiCard(pnlKpis, 1, "ЗАМОВЛЕНЬ У РЕСТОРАНІ", "0", ThemePalette.Primary);
        _lblAverageCheck = CreateKpiCard(pnlKpis, 2, "СЕРЕДНІЙ ЧЕК", "0.00 ₴", ThemePalette.Warning);
        _lblOccupancy = CreateKpiCard(pnlKpis, 3, "ЗАВАНТАЖЕНІСТЬ ГОТЕЛЮ", "0%", ThemePalette.Info);

        // ScottPlot Chart
        _plotRevenue = new FormsPlot
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark
        };

        Controls.Add(_plotRevenue);
        Controls.Add(pnlKpis);

        Load += async (s, e) => await LoadAnalyticsAsync();
    }

    private static Label CreateKpiCard(TableLayoutPanel parent, int col, string title, string initialValue, Color accentColor)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Margin = new Padding(6),
            Padding = new Padding(10)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 22
        };

        var lblVal = new Label
        {
            Text = initialValue,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = accentColor,
            Dock = DockStyle.Fill
        };

        pnl.Controls.Add(lblVal);
        pnl.Controls.Add(lblTitle);
        parent.Controls.Add(pnl, col, 0);

        return lblVal;
    }

    private async Task LoadAnalyticsAsync()
    {
        var data = await _analyticsService.GetExecutiveSummaryAsync();

        _lblTodayRevenue.Text = $"{data.TodayRevenue:N0} ₴";
        _lblTodayOrders.Text = data.TodayOrdersCount.ToString();
        _lblAverageCheck.Text = $"{data.AverageCheck:N0} ₴";
        _lblOccupancy.Text = $"{data.HotelOccupancyPercent:F1}%";

        // Render ScottPlot 5 Bar Chart for 7 Days Trend
        _plotRevenue.Plot.Clear();
        _plotRevenue.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#121826");
        _plotRevenue.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#1C2436");
        _plotRevenue.Plot.Axes.Color(ScottPlot.Color.FromHex("#94A3B8"));

        double[] restoRevenues = data.RevenueTrends.Select(t => (double)t.RestaurantRevenue).ToArray();
        double[] hotelRevenues = data.RevenueTrends.Select(t => (double)t.HotelRevenue).ToArray();

        var barsResto = _plotRevenue.Plot.Add.Bars(restoRevenues);
        barsResto.LegendText = "Ресторан";
        barsResto.Color = ScottPlot.Color.FromHex("#38BDF8");

        _plotRevenue.Plot.Title("Динаміка виручки HoReCa за останні 7 днів (₴)");
        _plotRevenue.Plot.ShowLegend();
        _plotRevenue.Refresh();
    }
}
