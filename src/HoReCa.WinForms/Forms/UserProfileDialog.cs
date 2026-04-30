using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;

namespace HoReCa.WinForms.Forms;

public class UserProfileDialog : Form
{
    private readonly IAuthService _authService;
    private readonly IStaffMonitoringService _staffService;

    public bool LogoutRequested { get; private set; }

    public UserProfileDialog(IAuthService authService, IStaffMonitoringService staffService)
    {
        _authService = authService;
        _staffService = staffService;

        Text = "Профіль співробітника & Безпека";
        Size = new Size(500, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ThemePalette.BackgroundDark;
        ForeColor = ThemePalette.TextPrimary;
        Font = ThemePalette.BodyFont;
        Padding = new Padding(20);

        var currentUser = _authService.CurrentUser;
        if (currentUser == null)
        {
            var lblNoUser = new Label
            {
                Text = "Користувач не авторизований у системі.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = ThemePalette.HeaderFont,
                ForeColor = ThemePalette.Warning
            };
            Controls.Add(lblNoUser);
            return;
        }

        // Top Profile Card
        var pnlUserCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(16)
        };

        var lblAvatar = new Label
        {
            Text = currentUser.Role switch
            {
                UserRole.Administrator => "👑",
                UserRole.Waiter => "🤵",
                UserRole.Chef => "👨‍🍳",
                UserRole.Barista => "☕",
                UserRole.Receptionist => "🏨",
                UserRole.Storekeeper => "📦",
                _ => "👤"
            },
            Font = new Font("Segoe UI Emoji", 32f),
            Size = new Size(65, 75),
            Location = new Point(16, 16),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblName = new Label
        {
            Text = currentUser.FullName,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Location = new Point(90, 18),
            Size = new Size(370, 24),
            AutoEllipsis = true
        };

        var lblRole = new Label
        {
            Text = $"Посада: {currentUser.Role}  |  Логін: @{currentUser.Username}",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(90, 46),
            Size = new Size(370, 20)
        };

        var lblStatus = new Label
        {
            Text = currentUser.IsActive ? "🟢 Акаунт активний" : "🔴 Заблоковано",
            Font = ThemePalette.SmallFont,
            ForeColor = currentUser.IsActive ? ThemePalette.Success : ThemePalette.Danger,
            Location = new Point(90, 70),
            Size = new Size(200, 20)
        };

        pnlUserCard.Controls.AddRange(new Control[] { lblAvatar, lblName, lblRole, lblStatus });

        // Shift Performance Section
        var grpShift = new GroupBox
        {
            Text = " ⏱️ Показники поточної зміни ",
            Dock = DockStyle.Top,
            Height = 140,
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.Primary,
            Padding = new Padding(14),
            Margin = new Padding(0, 14, 0, 0)
        };

        var lblShiftInfo = new Label
        {
            Text = "Завантаження даних зміни...",
            Font = ThemePalette.BodyFont,
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Fill
        };
        grpShift.Controls.Add(lblShiftInfo);

        // PIN Change Section
        var grpPin = new GroupBox
        {
            Text = " 🔒 Зміна PIN-коду швидкого входу ",
            Dock = DockStyle.Top,
            Height = 160,
            Font = ThemePalette.TitleFont,
            ForeColor = ThemePalette.Primary,
            Padding = new Padding(14),
            Margin = new Padding(0, 14, 0, 0)
        };

        var lblOldPin = new Label { Text = "Старий PIN:", Font = ThemePalette.SmallFont, ForeColor = ThemePalette.TextMuted, Location = new Point(14, 30), Size = new Size(120, 20) };
        var txtOldPin = new TextBox { Location = new Point(140, 28), Size = new Size(120, 24), PasswordChar = '•', MaxLength = 4, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

        var lblNewPin = new Label { Text = "Новий PIN (4 цифри):", Font = ThemePalette.SmallFont, ForeColor = ThemePalette.TextMuted, Location = new Point(14, 62), Size = new Size(120, 20) };
        var txtNewPin = new TextBox { Location = new Point(140, 60), Size = new Size(120, 24), PasswordChar = '•', MaxLength = 4, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

        var btnChangePin = new Button
        {
            Text = "💾 Зберегти новий PIN",
            Location = new Point(140, 96),
            Size = new Size(170, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand
        };
        btnChangePin.FlatAppearance.BorderColor = ThemePalette.Border;
        btnChangePin.Click += async (s, e) =>
        {
            if (txtNewPin.Text.Length != 4 || !txtNewPin.Text.All(char.IsDigit))
            {
                MessageBox.Show("Новий PIN повинен складатись рівно з 4 цифр!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var success = await _authService.ChangePinAsync(currentUser.Id, txtNewPin.Text);
            if (success)
            {
                MessageBox.Show("PIN-код успішно оновлено!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtOldPin.Clear();
                txtNewPin.Clear();
            }
            else
            {
                MessageBox.Show("Помилка оновлення PIN-коду.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        grpPin.Controls.AddRange(new Control[] { lblOldPin, txtOldPin, lblNewPin, txtNewPin, btnChangePin });

        // Bottom Action Buttons
        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnClose = new Button
        {
            Text = "Закрити",
            Size = new Size(95, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundCard,
            ForeColor = ThemePalette.TextPrimary,
            Font = ThemePalette.BodyFont,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderColor = ThemePalette.Border;
        btnClose.Click += (s, e) => Close();

        var btnLogout = new Button
        {
            Text = "🚪 Вийти з акаунта",
            Size = new Size(150, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.BodyFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnLogout.FlatAppearance.BorderColor = ThemePalette.Danger;
        btnLogout.Click += (s, e) =>
        {
            LogoutRequested = true;
            _authService.Logout();
            Close();
        };

        pnlBottom.Controls.AddRange(new Control[] { btnClose, btnLogout });

        // Add controls in reverse dock order
        Controls.Add(pnlBottom);
        Controls.Add(grpPin);
        Controls.Add(grpShift);
        Controls.Add(pnlUserCard);

        Load += async (s, e) =>
        {
            var shifts = await _staffService.GetActiveShiftsAsync();
            var shift = shifts.FirstOrDefault(s => s.UserId == currentUser.Id);
            if (shift != null)
            {
                var hours = (DateTime.UtcNow - shift.ClockInTime).TotalHours;
                lblShiftInfo.Text = $"• Статус зміни: 🟢 Активна (з {shift.ClockInTime:HH:mm})\n" +
                                   $"• Відпрацьовано: {hours:F1} год.\n" +
                                   $"• Обслуговано замовлень: {shift.OrdersProcessed}\n" +
                                   $"• Оформлений виторг: {shift.TotalSales:N2} ₴\n" +
                                   $"• Нараховані чайові: {shift.TipsEarned:N2} ₴";
            }
            else
            {
                lblShiftInfo.Text = "• Статус зміни: ⚪ Зміна не відкрита.\n\nЩоб зафіксувати початок зміни, відкрийте меню «Персонал & Зміни» та натисніть «Моя зміна».";
            }
        };
    }
}
