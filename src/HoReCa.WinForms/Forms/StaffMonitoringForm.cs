using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Forms;

public class StaffMonitoringForm : BaseForm
{
    private readonly IStaffMonitoringService _staffService;
    private readonly IAuthService _authService;

    private readonly FlowLayoutPanel _pnlKpiCards;
    private readonly DataGridView _gridChefs;
    private readonly DataGridView _gridWaiters;
    private readonly Label _lblLastUpdated;

    public StaffMonitoringForm(IStaffMonitoringService staffService, IAuthService authService)
    {
        _staffService = staffService;
        _authService = authService;

        Padding = new Padding(16);
        BackColor = ThemePalette.BackgroundDark;

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(16, 10, 16, 10)
        };

        var lblTitle = new Label
        {
            Text = "👥 МОНІТОРИНГ ПЕРСОНАЛУ & ЖИВІ ЗМІНИ",
            Font = ThemePalette.HeaderFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 24,
            AutoEllipsis = true
        };

        var lblSubtitle = new Label
        {
            Text = "Контроль навантаження кухонної бригади, результативність залу та облік робочого часу",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 18,
            AutoEllipsis = true
        };

        var pnlHeaderActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnRefresh = new Button
        {
            Text = "🔄 Оновити дані",
            UseMnemonic = false,
            Height = 36,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.BodyFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 2, 0, 0)
        };
        btnRefresh.FlatAppearance.BorderColor = ThemePalette.Border;
        btnRefresh.Click += async (s, e) => await LoadDashboardDataAsync();

        var btnClockToggle = new Button
        {
            Text = "⏱️ Моя зміна (Clock In/Out)",
            UseMnemonic = false,
            Height = 36,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.PrimaryDark,
            ForeColor = Color.White,
            Font = ThemePalette.BodyFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 2, 0, 0)
        };
        btnClockToggle.FlatAppearance.BorderSize = 0;
        btnClockToggle.Click += async (s, e) => await ToggleCurrentUserShiftAsync();

        _lblLastUpdated = new Label
        {
            Text = "Оновлено: —",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 10, 12, 0)
        };

        pnlHeaderActions.Controls.AddRange(new Control[] { btnClockToggle, btnRefresh, _lblLastUpdated });

        pnlHeader.Controls.Add(pnlHeaderActions);
        pnlHeader.Controls.Add(lblSubtitle);
        pnlHeader.Controls.Add(lblTitle);

        // KPI Summary Cards Panel
        _pnlKpiCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = ThemePalette.BackgroundDark,
            Padding = new Padding(0, 10, 0, 10),
            WrapContents = false,
            AutoScroll = true
        };

        // Main Split: Kitchen Crew vs Floor Waiters (Dynamic 50/50 responsive split)
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 8,
            BackColor = ThemePalette.BackgroundDark
        };
        split.SizeChanged += (s, e) =>
        {
            if (split.Width > 400)
            {
                split.SplitterDistance = split.Width / 2;
            }
        };

        // LEFT: Kitchen Brigade
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(12)
        };

        var lblKitchenTitle = new Label
        {
            Text = "👨‍🍳 КУХОННА БРИГАДА & НАВАНТАЖЕННЯ ЦЕХІВ",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        _gridChefs = CreateStyledGrid();
        _gridChefs.Columns.Add("FullName", "Кухар / Посада");
        _gridChefs.Columns.Add("Station", "Станція / Цех");
        _gridChefs.Columns.Add("Hours", "Час у зміні");
        _gridChefs.Columns.Add("Dishes", "Приготовано");
        _gridChefs.Columns.Add("Status", "Статус");

        _gridChefs.Columns[0].FillWeight = 34;
        _gridChefs.Columns[0].MinimumWidth = 140;
        _gridChefs.Columns[1].FillWeight = 24;
        _gridChefs.Columns[1].MinimumWidth = 100;
        _gridChefs.Columns[2].FillWeight = 16;
        _gridChefs.Columns[2].MinimumWidth = 75;
        _gridChefs.Columns[3].FillWeight = 14;
        _gridChefs.Columns[3].MinimumWidth = 80;
        _gridChefs.Columns[4].FillWeight = 12;
        _gridChefs.Columns[4].MinimumWidth = 70;

        pnlLeft.Controls.Add(_gridChefs);
        pnlLeft.Controls.Add(lblKitchenTitle);
        split.Panel1.Controls.Add(pnlLeft);

        // RIGHT: Floor Service (Waiters & Baristas)
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(12)
        };

        var lblWaitersTitle = new Label
        {
            Text = "🤵 ОБСЛУГОВУЮЧИЙ ПЕРСОНАЛ (ОФІЦІАНТИ & БАР)",
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        _gridWaiters = CreateStyledGrid();
        _gridWaiters.Columns.Add("FullName", "Офіціант / Бармен");
        _gridWaiters.Columns.Add("Zone", "Зона залу");
        _gridWaiters.Columns.Add("Orders", "Чеки");
        _gridWaiters.Columns.Add("Sales", "Виторг");
        _gridWaiters.Columns.Add("Tips", "Чайові");

        _gridWaiters.Columns[0].FillWeight = 32;
        _gridWaiters.Columns[0].MinimumWidth = 140;
        _gridWaiters.Columns[1].FillWeight = 23;
        _gridWaiters.Columns[1].MinimumWidth = 95;
        _gridWaiters.Columns[2].FillWeight = 15;
        _gridWaiters.Columns[2].MinimumWidth = 55;
        _gridWaiters.Columns[3].FillWeight = 16;
        _gridWaiters.Columns[3].MinimumWidth = 80;
        _gridWaiters.Columns[4].FillWeight = 14;
        _gridWaiters.Columns[4].MinimumWidth = 65;

        pnlRight.Controls.Add(_gridWaiters);
        pnlRight.Controls.Add(lblWaitersTitle);
        split.Panel2.Controls.Add(pnlRight);

        // Compose Form
        Controls.Add(split);
        Controls.Add(_pnlKpiCards);
        Controls.Add(pnlHeader);

        Load += async (s, e) => await LoadDashboardDataAsync();
    }

    private static DataGridView CreateStyledGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemePalette.BackgroundCard,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
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

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            var data = await _staffService.GetStaffDashboardAsync();

            _lblLastUpdated.Text = $"Оновлено: {DateTime.Now:HH:mm:ss}";

            decimal totalSales = data.Waiters.Sum(w => w.SalesToday);
            decimal totalTips = data.Waiters.Sum(w => w.TipsToday);

            // Render KPI cards
            _pnlKpiCards.Controls.Clear();
            _pnlKpiCards.Controls.Add(CreateKpiCard("КУХАРІВ НА ЗМІНІ", $"{data.KitchenCrewCount} осіб", ThemePalette.Primary));
            _pnlKpiCards.Controls.Add(CreateKpiCard("СТРАВ У РОБОТІ", $"{data.ActiveOrdersInKitchen} страв", ThemePalette.Warning));
            _pnlKpiCards.Controls.Add(CreateKpiCard("ОФІЦІАНТІВ У ЗАЛІ", $"{data.WaitersOnFloorCount} осіб", ThemePalette.Success));
            _pnlKpiCards.Controls.Add(CreateKpiCard("ВИТОРГ ЗА ЗМІНУ", $"{totalSales:N0} ₴", ThemePalette.Success));
            _pnlKpiCards.Controls.Add(CreateKpiCard("ЧАЙОВІ ПЕРСОНАЛУ", $"{totalTips:N0} ₴", ThemePalette.Info));

            // Populate Kitchen Brigade Grid
            _gridChefs.Rows.Clear();
            foreach (var chef in data.KitchenStaff)
            {
                int rowIndex = _gridChefs.Rows.Add(
                    chef.FullName,
                    chef.Station,
                    $"{chef.ActiveOrdersCount} в роботі",
                    $"{chef.CompletedTodayCount} страв",
                    chef.IsOnShift ? "🟢 На зміні" : "⚪ Зміна закрита"
                );

                if (chef.IsOnShift)
                {
                    _gridChefs.Rows[rowIndex].Cells[4].Style.ForeColor = ThemePalette.Success;
                }
            }

            // Populate Waiters Grid
            _gridWaiters.Rows.Clear();
            foreach (var waiter in data.Waiters)
            {
                int rowIndex = _gridWaiters.Rows.Add(
                    waiter.FullName,
                    waiter.AssignedZone,
                    $"{waiter.OrdersServedToday} замовл.",
                    $"{waiter.SalesToday:N0} ₴",
                    $"{waiter.TipsToday:N0} ₴"
                );

                _gridWaiters.Rows[rowIndex].Cells[3].Style.ForeColor = ThemePalette.Success;
                _gridWaiters.Rows[rowIndex].Cells[4].Style.ForeColor = ThemePalette.Info;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка завантаження даних моніторингу: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Control CreateKpiCard(string title, string value, Color valueColor)
    {
        var pnl = new Panel
        {
            Width = 205,
            Height = 78,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 12, 0)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var lblVal = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = valueColor,
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        pnl.Controls.AddRange(new Control[] { lblVal, lblTitle });
        return pnl;
    }

    private async Task ToggleCurrentUserShiftAsync()
    {
        var currentUser = _authService.CurrentUser;
        if (currentUser == null)
        {
            MessageBox.Show("Будь ласка, авторизуйтесь у системі для фіксації зміни.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var activeShifts = await _staffService.GetActiveShiftsAsync();
        var activeShift = activeShifts.FirstOrDefault(s => s.UserId == currentUser.Id);
        if (activeShift == null)
        {
            // Clock In
            await _staffService.ClockInAsync(currentUser.Id, "Основний зал / Станція");
            MessageBox.Show($"Зміну успішно відкрито для {currentUser.FullName} о {DateTime.Now:HH:mm}!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            // Clock Out
            var hours = (DateTime.UtcNow - activeShift.ClockInTime).TotalHours;
            var confirm = MessageBox.Show(
                $"Закрити поточну зміну для {currentUser.FullName}?\nВідпрацьовано: {hours:F1} год.\nВиторг: {activeShift.TotalSales:N2} ₴",
                "Підтвердження закриття зміни",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                await _staffService.ClockOutAsync(activeShift.Id);
                MessageBox.Show("Зміну закрито. Дякуємо за плідну роботу!", "Інфо", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        await LoadDashboardDataAsync();
    }
}
