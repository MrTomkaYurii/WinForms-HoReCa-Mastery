using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.WinForms.Common;
using ScottPlot.WinForms;

namespace HoReCa.WinForms.Forms;

public enum AnalyticsTab
{
    RevenueDynamics,
    TableTurnover,
    StaffPerformance,
    PopularDishes
}

public class AnalyticsForm : BaseForm
{
    private readonly IAnalyticsService _analyticsService;
    private readonly FormsPlot _plotRevenue;
    private readonly Label _lblTodayRevenue;
    private readonly Label _lblTodayOrders;
    private readonly Label _lblAverageCheck;
    private readonly Label _lblOccupancy;

    private readonly Button _btnTabRevenue;
    private readonly Button _btnTabTables;
    private readonly Button _btnTabStaff;
    private readonly Button _btnTabDishes;
    private readonly Button _btnRefresh;

    private readonly SplitContainer _splitMain;
    private readonly DataGridView _gridDetails;
    private readonly Label _lblGridTitle;
    private readonly Label _lblGridFooter;

    private AnalyticsSummaryDto? _currentData;
    private AnalyticsTab _activeTab = AnalyticsTab.RevenueDynamics;

    public AnalyticsForm(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;

        Text = "Аналітика та фінансовий моніторинг | HoReCa System";
        BackColor = ThemePalette.BackgroundDark;
        Padding = new Padding(12);

        // 1. Top KPI Dashboard Panel
        var pnlKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 105,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 8)
        };

        for (int i = 0; i < 4; i++)
        {
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        }

        _lblTodayRevenue = CreateKpiCard(pnlKpis, 0, "ВИРУЧКА СЬОГОДНІ", "0.00 ₴", "Ресторан + Готель", ThemePalette.Success);
        _lblTodayOrders = CreateKpiCard(pnlKpis, 1, "ЗАМОВЛЕНЬ У РЕСТОРАНІ", "0", "Оплачено гостями", ThemePalette.Primary);
        _lblAverageCheck = CreateKpiCard(pnlKpis, 2, "СЕРЕДНІЙ ЧЕК СТОЛУ", "0.00 ₴", "Цільовий: 750 ₴", ThemePalette.Warning);
        _lblOccupancy = CreateKpiCard(pnlKpis, 3, "ЗАВАНТАЖЕНІСТЬ ГОТЕЛЮ", "0%", "Номерний фонд", ThemePalette.Info);

        // 2. Navigation Tab Bar Panel
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(8, 6, 8, 6)
        };

        var pnlTabsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _btnTabRevenue = CreateTabButton("📈 Динаміка виручки (7 днів)", AnalyticsTab.RevenueDynamics);
        _btnTabTables = CreateTabButton("🪑 Оборотність столиків", AnalyticsTab.TableTurnover);
        _btnTabStaff = CreateTabButton("🤵 Персонал та чайові", AnalyticsTab.StaffPerformance);
        _btnTabDishes = CreateTabButton("🍕 Топ страв та меню", AnalyticsTab.PopularDishes);

        pnlTabsFlow.Controls.AddRange(new Control[] { _btnTabRevenue, _btnTabTables, _btnTabStaff, _btnTabDishes });

        _btnRefresh = new Button
        {
            Text = "🔄 Оновити дані",
            Font = ThemePalette.BodyFont,
            ForeColor = ThemePalette.TextPrimary,
            BackColor = ThemePalette.BackgroundCard,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 38),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            UseMnemonic = false
        };
        _btnRefresh.FlatAppearance.BorderColor = ThemePalette.Border;
        _btnRefresh.Click += async (s, e) => await LoadAnalyticsAsync();

        pnlToolbar.Controls.Add(pnlTabsFlow);
        pnlToolbar.Controls.Add(_btnRefresh);

        // 3. Main Split Area: Chart (Left) + Details Table (Right)
        _splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = ThemePalette.Border,
            SplitterWidth = 6,
            Margin = new Padding(0, 8, 0, 0)
        };

        // Left: ScottPlot 5 Chart
        _plotRevenue = new FormsPlot
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark
        };
        _splitMain.Panel1.Controls.Add(_plotRevenue);
        _splitMain.Panel1.BackColor = ThemePalette.BackgroundDark;
        _splitMain.Panel1.Padding = new Padding(4);

        // Right: Details Panel with Header, Grid, and Footer
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(10)
        };

        _lblGridTitle = new Label
        {
            Text = "📊 ДЕТАЛІЗАЦІЯ ПОКАЗНИКІВ",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        _lblGridFooter = new Label
        {
            Text = "Підсумок за вибраний період",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Bottom,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _gridDetails = CreateStyledGrid();
        _gridDetails.Dock = DockStyle.Fill;

        pnlRight.Controls.Add(_gridDetails);
        pnlRight.Controls.Add(_lblGridTitle);
        pnlRight.Controls.Add(_lblGridFooter);

        _splitMain.Panel2.Controls.Add(pnlRight);
        _splitMain.Panel2.BackColor = ThemePalette.BackgroundDark;

        // Assembly
        Controls.Add(_splitMain);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlKpis);

        Resize += (s, e) =>
        {
            if (Width > 800)
            {
                try
                {
                    _splitMain.SplitterDistance = Math.Max(400, (int)(Width * 0.60));
                }
                catch
                {
                    // Ignore layout transition exceptions
                }
            }
        };

        Load += async (s, e) =>
        {
            if (Width > 800)
            {
                _splitMain.SplitterDistance = Math.Max(400, (int)(Width * 0.60));
            }
            await LoadAnalyticsAsync();
        };
    }

    private Button CreateTabButton(string text, AnalyticsTab tab)
    {
        var btn = new Button
        {
            Text = text,
            Font = ThemePalette.BodyFont,
            ForeColor = ThemePalette.TextMuted,
            BackColor = ThemePalette.BackgroundCard,
            FlatStyle = FlatStyle.Flat,
            Height = 38,
            AutoSize = true,
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            UseMnemonic = false
        };
        btn.FlatAppearance.BorderColor = ThemePalette.Border;

        btn.Click += (s, e) => SwitchTab(tab);
        return btn;
    }

    private void SwitchTab(AnalyticsTab tab)
    {
        _activeTab = tab;

        // Update button visual styles
        UpdateButtonState(_btnTabRevenue, _activeTab == AnalyticsTab.RevenueDynamics);
        UpdateButtonState(_btnTabTables, _activeTab == AnalyticsTab.TableTurnover);
        UpdateButtonState(_btnTabStaff, _activeTab == AnalyticsTab.StaffPerformance);
        UpdateButtonState(_btnTabDishes, _activeTab == AnalyticsTab.PopularDishes);

        if (_currentData != null)
        {
            RenderActiveTab(_currentData);
        }
    }

    private static void UpdateButtonState(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.BackColor = ThemePalette.PrimaryDark;
            btn.ForeColor = Color.White;
            btn.Font = new Font(ThemePalette.BodyFont, FontStyle.Bold);
            btn.FlatAppearance.BorderColor = ThemePalette.Primary;
        }
        else
        {
            btn.BackColor = ThemePalette.BackgroundCard;
            btn.ForeColor = ThemePalette.TextMuted;
            btn.Font = new Font(ThemePalette.BodyFont, FontStyle.Regular);
            btn.FlatAppearance.BorderColor = ThemePalette.Border;
        }
    }

    private static Label CreateKpiCard(TableLayoutPanel parent, int col, string title, string initialValue, string subtitle, Color accentColor)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Margin = new Padding(6),
            Padding = new Padding(12, 8, 12, 8)
        };

        var lblSub = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Bottom,
            Height = 16
        };

        var lblVal = new Label
        {
            Text = initialValue,
            Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
            ForeColor = accentColor,
            Dock = DockStyle.Fill
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 18
        };

        pnl.Controls.Add(lblVal);
        pnl.Controls.Add(lblSub);
        pnl.Controls.Add(lblTitle);
        parent.Controls.Add(pnl, col, 0);

        return lblVal;
    }

    private static DataGridView CreateStyledGrid()
    {
        var grid = new DataGridView
        {
            BackgroundColor = ThemePalette.BackgroundCard,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            GridColor = ThemePalette.Border,
            RowTemplate = { Height = 34 }
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = ThemePalette.BackgroundSidebar;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.Font = ThemePalette.TitleFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4);
        grid.ColumnHeadersHeight = 36;

        grid.DefaultCellStyle.BackColor = ThemePalette.BackgroundCard;
        grid.DefaultCellStyle.ForeColor = ThemePalette.TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = ThemePalette.BackgroundInput;
        grid.DefaultCellStyle.SelectionForeColor = ThemePalette.Primary;
        grid.DefaultCellStyle.Font = ThemePalette.BodyFont;
        grid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);

        return grid;
    }

    private async Task LoadAnalyticsAsync()
    {
        try
        {
            _currentData = await _analyticsService.GetExecutiveSummaryAsync();

            _lblTodayRevenue.Text = $"{_currentData.TodayRevenue:N0} ₴";
            _lblTodayOrders.Text = _currentData.TodayOrdersCount.ToString();
            _lblAverageCheck.Text = $"{_currentData.AverageCheck:N0} ₴";
            _lblOccupancy.Text = $"{_currentData.HotelOccupancyPercent:F1}%";

            SwitchTab(_activeTab);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка завантаження аналітики: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyPlotTheme()
    {
        _plotRevenue.Plot.Clear();
        _plotRevenue.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#121216");
        _plotRevenue.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#18181E");
        _plotRevenue.Plot.Axes.Color(ScottPlot.Color.FromHex("#94A3B8"));
        _plotRevenue.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#262630");

        _plotRevenue.Plot.Legend.BackgroundColor = ScottPlot.Color.FromHex("#1C1C22");
        _plotRevenue.Plot.Legend.FontColor = ScottPlot.Color.FromHex("#E2E8F0");
        _plotRevenue.Plot.Legend.OutlineColor = ScottPlot.Color.FromHex("#334155");
    }

    private void RenderActiveTab(AnalyticsSummaryDto data)
    {
        ApplyPlotTheme();

        switch (_activeTab)
        {
            case AnalyticsTab.RevenueDynamics:
                RenderRevenueDynamics(data);
                break;
            case AnalyticsTab.TableTurnover:
                RenderTableTurnover(data);
                break;
            case AnalyticsTab.StaffPerformance:
                RenderStaffPerformance(data);
                break;
            case AnalyticsTab.PopularDishes:
                RenderPopularDishes(data);
                break;
        }

        _plotRevenue.Plot.Axes.AutoScale();
        _plotRevenue.Refresh();
    }

    private void RenderRevenueDynamics(AnalyticsSummaryDto data)
    {
        _lblGridTitle.Text = "📈 ДИНАМІКА ДОХОДІВ: РЕСТОРАН ТА ГОТЕЛЬ ЗА 7 ДНІВ";

        var barsResto = new List<ScottPlot.Bar>();
        var barsHotel = new List<ScottPlot.Bar>();
        var ticks = new ScottPlot.TickGenerators.NumericManual();

        _gridDetails.Columns.Clear();
        _gridDetails.Rows.Clear();
        _gridDetails.Columns.Add("Date", "Дата / День");
        _gridDetails.Columns.Add("Resto", "Ресторан (₴)");
        _gridDetails.Columns.Add("Hotel", "Готель (₴)");
        _gridDetails.Columns.Add("Total", "Разом (₴)");
        _gridDetails.Columns.Add("RestoShare", "Частка закладу");

        _gridDetails.Columns[0].FillWeight = 28;
        _gridDetails.Columns[1].FillWeight = 20;
        _gridDetails.Columns[2].FillWeight = 20;
        _gridDetails.Columns[3].FillWeight = 20;
        _gridDetails.Columns[4].FillWeight = 12;

        _gridDetails.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridDetails.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridDetails.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridDetails.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        decimal totalResto = 0;
        decimal totalHotel = 0;

        for (int i = 0; i < data.RevenueTrends.Count; i++)
        {
            var trend = data.RevenueTrends[i];
            totalResto += trend.RestaurantRevenue;
            totalHotel += trend.HotelRevenue;
            decimal dayTotal = trend.RestaurantRevenue + trend.HotelRevenue;
            double share = dayTotal > 0 ? (double)(trend.RestaurantRevenue / dayTotal * 100) : 0;

            barsResto.Add(new ScottPlot.Bar
            {
                Position = i - 0.18,
                Value = (double)trend.RestaurantRevenue,
                FillColor = ScottPlot.Color.FromHex("#38BDF8"),
                LineColor = ScottPlot.Color.FromHex("#0284C7"),
                Size = 0.32
            });

            barsHotel.Add(new ScottPlot.Bar
            {
                Position = i + 0.18,
                Value = (double)trend.HotelRevenue,
                FillColor = ScottPlot.Color.FromHex("#34D399"),
                LineColor = ScottPlot.Color.FromHex("#059669"),
                Size = 0.32
            });

            string dayLabel = $"{trend.Date:dd.MM} ({trend.Date:ddd})";
            ticks.AddMajor(i, dayLabel);

            int rIdx = _gridDetails.Rows.Add(
                dayLabel,
                $"{trend.RestaurantRevenue:N0} ₴",
                $"{trend.HotelRevenue:N0} ₴",
                $"{dayTotal:N0} ₴",
                $"{share:F1}%"
            );

            _gridDetails.Rows[rIdx].Cells[1].Style.ForeColor = ThemePalette.Primary;
            _gridDetails.Rows[rIdx].Cells[2].Style.ForeColor = ThemePalette.Success;
            _gridDetails.Rows[rIdx].Cells[3].Style.ForeColor = ThemePalette.TextPrimary;
            _gridDetails.Rows[rIdx].Cells[3].Style.Font = new Font(ThemePalette.BodyFont, FontStyle.Bold);
        }

        var bPlotResto = _plotRevenue.Plot.Add.Bars(barsResto);
        bPlotResto.LegendText = "Ресторан";

        var bPlotHotel = _plotRevenue.Plot.Add.Bars(barsHotel);
        bPlotHotel.LegendText = "Готель";

        _plotRevenue.Plot.Axes.Bottom.TickGenerator = ticks;
        _plotRevenue.Plot.Axes.Bottom.TickLabelStyle.Rotation = -20;
        _plotRevenue.Plot.Title("Динаміка виручки HoReCa за останні 7 днів (₴)");
        _plotRevenue.Plot.ShowLegend();

        _lblGridFooter.Text = $"Підсумок за 7 днів: Ресторан {totalResto:N0} ₴ | Готель {totalHotel:N0} ₴ | Всього: {totalResto + totalHotel:N0} ₴";
    }

    private void RenderTableTurnover(AnalyticsSummaryDto data)
    {
        _lblGridTitle.Text = "🪑 ОБОРОТНІСТЬ ТА ВИТОРГ ПО СТОЛИКАХ ЗАЛУ";

        var bars = new List<ScottPlot.Bar>();
        var ticks = new ScottPlot.TickGenerators.NumericManual();

        _gridDetails.Columns.Clear();
        _gridDetails.Rows.Clear();
        _gridDetails.Columns.Add("Table", "Стіл");
        _gridDetails.Columns.Add("Zone", "Зала / Зона");
        _gridDetails.Columns.Add("Seats", "Місць");
        _gridDetails.Columns.Add("Orders", "Замовлень");
        _gridDetails.Columns.Add("Revenue", "Виторг (₴)");
        _gridDetails.Columns.Add("AvgCheck", "Сер. чек (₴)");

        _gridDetails.Columns[0].FillWeight = 14;
        _gridDetails.Columns[1].FillWeight = 26;
        _gridDetails.Columns[2].FillWeight = 12;
        _gridDetails.Columns[3].FillWeight = 16;
        _gridDetails.Columns[4].FillWeight = 18;
        _gridDetails.Columns[5].FillWeight = 14;

        _gridDetails.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDetails.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDetails.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDetails.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridDetails.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        decimal totalRevenue = 0;
        int totalOrders = 0;

        for (int i = 0; i < data.TableActivities.Count; i++)
        {
            var t = data.TableActivities[i];
            totalRevenue += t.TotalRevenue;
            totalOrders += t.OrdersCount;

            ScottPlot.Color barColor = t.ZoneName.Contains("VIP") ? ScottPlot.Color.FromHex("#F59E0B")
                : t.ZoneName.Contains("Тераса") ? ScottPlot.Color.FromHex("#10B981")
                : t.ZoneName.Contains("Банкет") ? ScottPlot.Color.FromHex("#A855F7")
                : ScottPlot.Color.FromHex("#38BDF8");

            bars.Add(new ScottPlot.Bar
            {
                Position = i,
                Value = (double)t.TotalRevenue,
                FillColor = barColor,
                LineColor = barColor,
                Size = 0.58
            });

            ticks.AddMajor(i, $"Стіл {t.TableNumber}");

            int rIdx = _gridDetails.Rows.Add(
                $"№ {t.TableNumber}",
                t.ZoneName,
                $"{t.Capacity} місць",
                t.OrdersCount,
                $"{t.TotalRevenue:N0} ₴",
                $"{t.AverageCheck:N0} ₴"
            );

            _gridDetails.Rows[rIdx].Cells[4].Style.ForeColor = ThemePalette.Success;
            _gridDetails.Rows[rIdx].Cells[4].Style.Font = new Font(ThemePalette.BodyFont, FontStyle.Bold);
            _gridDetails.Rows[rIdx].Cells[5].Style.ForeColor = ThemePalette.Warning;
        }

        var bPlot = _plotRevenue.Plot.Add.Bars(bars);
        bPlot.LegendText = "Виторг столу (₴)";

        _plotRevenue.Plot.Axes.Bottom.TickGenerator = ticks;
        _plotRevenue.Plot.Axes.Bottom.TickLabelStyle.Rotation = -35;
        _plotRevenue.Plot.Title("Завантаженість та виторг ресторану по столиках (₴)");
        _plotRevenue.Plot.ShowLegend();

        decimal avgCheckOverall = totalOrders > 0 ? totalRevenue / totalOrders : 0;
        _lblGridFooter.Text = $"Столиків: {data.TableActivities.Count} | Замовлень: {totalOrders} | Загальний виторг: {totalRevenue:N0} ₴ | Сер. чек: {avgCheckOverall:N0} ₴";
    }

    private void RenderStaffPerformance(AnalyticsSummaryDto data)
    {
        _lblGridTitle.Text = "🤵 ПРОДУКТИВНІСТЬ ОФІЦІАНТІВ ТА ЧАЙОВІ ЗА ЗМІНУ";

        var barsSales = new List<ScottPlot.Bar>();
        var barsTips = new List<ScottPlot.Bar>();
        var ticks = new ScottPlot.TickGenerators.NumericManual();

        _gridDetails.Columns.Clear();
        _gridDetails.Rows.Clear();
        _gridDetails.Columns.Add("Name", "Офіціант / Бармен");
        _gridDetails.Columns.Add("Zone", "Зона залу");
        _gridDetails.Columns.Add("Orders", "Замовлень");
        _gridDetails.Columns.Add("Sales", "Виторг (₴)");
        _gridDetails.Columns.Add("Tips", "Чайові (₴)");
        _gridDetails.Columns.Add("Percent", "% чайових");

        _gridDetails.Columns[0].FillWeight = 26;
        _gridDetails.Columns[1].FillWeight = 24;
        _gridDetails.Columns[2].FillWeight = 14;
        _gridDetails.Columns[3].FillWeight = 18;
        _gridDetails.Columns[4].FillWeight = 18;
        _gridDetails.Columns[5].FillWeight = 12;

        _gridDetails.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDetails.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridDetails.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridDetails.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        decimal totalSales = 0;
        decimal totalTips = 0;

        for (int i = 0; i < data.WaiterStats.Count; i++)
        {
            var w = data.WaiterStats[i];
            totalSales += w.SalesToday;
            totalTips += w.TipsToday;
            double tipPercent = w.SalesToday > 0 ? (double)(w.TipsToday / w.SalesToday * 100) : 0;

            barsSales.Add(new ScottPlot.Bar
            {
                Position = i - 0.18,
                Value = (double)w.SalesToday,
                FillColor = ScottPlot.Color.FromHex("#38BDF8"),
                LineColor = ScottPlot.Color.FromHex("#0284C7"),
                Size = 0.32
            });

            barsTips.Add(new ScottPlot.Bar
            {
                Position = i + 0.18,
                Value = (double)w.TipsToday,
                FillColor = ScottPlot.Color.FromHex("#F59E0B"),
                LineColor = ScottPlot.Color.FromHex("#D97706"),
                Size = 0.32
            });

            ticks.AddMajor(i, w.FullName);

            int rIdx = _gridDetails.Rows.Add(
                w.FullName,
                w.AssignedZone,
                $"{w.OrdersServedToday} чек.",
                $"{w.SalesToday:N0} ₴",
                $"{w.TipsToday:N0} ₴",
                $"{tipPercent:F1}%"
            );

            _gridDetails.Rows[rIdx].Cells[3].Style.ForeColor = ThemePalette.Success;
            _gridDetails.Rows[rIdx].Cells[3].Style.Font = new Font(ThemePalette.BodyFont, FontStyle.Bold);
            _gridDetails.Rows[rIdx].Cells[4].Style.ForeColor = ThemePalette.Warning;
        }

        var bPlotSales = _plotRevenue.Plot.Add.Bars(barsSales);
        bPlotSales.LegendText = "Виторг продажів";

        var bPlotTips = _plotRevenue.Plot.Add.Bars(barsTips);
        bPlotTips.LegendText = "Чайові персоналу";

        _plotRevenue.Plot.Axes.Bottom.TickGenerator = ticks;
        _plotRevenue.Plot.Axes.Bottom.TickLabelStyle.Rotation = -15;
        _plotRevenue.Plot.Title("Результативність залу: Продажі та чайові офіціантів (₴)");
        _plotRevenue.Plot.ShowLegend();

        _lblGridFooter.Text = $"Персоналу: {data.WaiterStats.Count} | Загальний виторг: {totalSales:N0} ₴ | Всього чайових: {totalTips:N0} ₴";
    }

    private void RenderPopularDishes(AnalyticsSummaryDto data)
    {
        _lblGridTitle.Text = "🍕 ТОП-8 ПОПУЛЯРНИХ СТРАВ РЕСТОРАНУ ТА КАТЕГОРІЙ";

        var bars = new List<ScottPlot.Bar>();
        var ticks = new ScottPlot.TickGenerators.NumericManual();

        _gridDetails.Columns.Clear();
        _gridDetails.Rows.Clear();
        _gridDetails.Columns.Add("Rank", "№");
        _gridDetails.Columns.Add("DishName", "Назва страви");
        _gridDetails.Columns.Add("Qty", "Продано (шт)");
        _gridDetails.Columns.Add("Revenue", "Виручка (₴)");

        _gridDetails.Columns[0].FillWeight = 10;
        _gridDetails.Columns[1].FillWeight = 50;
        _gridDetails.Columns[2].FillWeight = 20;
        _gridDetails.Columns[3].FillWeight = 20;

        _gridDetails.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDetails.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDetails.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        decimal totalDishRev = 0;
        int totalDishQty = 0;

        for (int i = 0; i < data.TopDishes.Count; i++)
        {
            var d = data.TopDishes[i];
            totalDishRev += d.TotalRevenue;
            totalDishQty += d.QuantitySold;

            bars.Add(new ScottPlot.Bar
            {
                Position = i,
                Value = (double)d.TotalRevenue,
                FillColor = ScottPlot.Color.FromHex("#818CF8"),
                LineColor = ScottPlot.Color.FromHex("#6366F1"),
                Size = 0.55
            });

            // Shorten name for tick label if needed
            string shortName = d.Name.Length > 18 ? d.Name[..16] + ".." : d.Name;
            ticks.AddMajor(i, shortName);

            int rIdx = _gridDetails.Rows.Add(
                $"#{i + 1}",
                d.Name,
                $"{d.QuantitySold} порц.",
                $"{d.TotalRevenue:N0} ₴"
            );

            _gridDetails.Rows[rIdx].Cells[3].Style.ForeColor = ThemePalette.Success;
            _gridDetails.Rows[rIdx].Cells[3].Style.Font = new Font(ThemePalette.BodyFont, FontStyle.Bold);
        }

        var bPlot = _plotRevenue.Plot.Add.Bars(bars);
        bPlot.LegendText = "Виручка від продажу (₴)";

        _plotRevenue.Plot.Axes.Bottom.TickGenerator = ticks;
        _plotRevenue.Plot.Axes.Bottom.TickLabelStyle.Rotation = -30;
        _plotRevenue.Plot.Title("Топ-8 страв ресторану за обсягом виручки (₴)");
        _plotRevenue.Plot.ShowLegend();

        _lblGridFooter.Text = $"Топ-8 страв: {totalDishQty} приготованих порцій на суму {totalDishRev:N0} ₴";
    }
}
