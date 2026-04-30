using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Application.Validators;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using Serilog;

namespace HoReCa.WinForms.Forms;

public class WelcomeStartForm : UserControl
{
    private readonly IAuthService _authService;
    private readonly RegisterUserRequestValidator _registerValidator = new();

    public event Action<UserAccountDto>? UserLoggedIn;

    // Login inputs
    private readonly TextBox _txtLoginUser;
    private readonly TextBox _txtLoginPass;
    private readonly Button _btnLogin;
    private readonly Label _lblLoginStatus;

    // Register inputs
    private readonly TextBox _txtRegFullName;
    private readonly TextBox _txtRegUser;
    private readonly TextBox _txtRegPass;
    private readonly TextBox _txtRegPin;
    private readonly ComboBox _cbRegRole;
    private readonly Button _btnRegister;
    private readonly Label _lblRegStatus;

    public WelcomeStartForm(IAuthService authService)
    {
        _authService = authService;

        DoubleBuffered = true;
        BackColor = ThemePalette.BackgroundDark;
        ForeColor = ThemePalette.TextPrimary;
        Font = ThemePalette.BodyFont;
        AutoScroll = true;

        // Centered Canvas Container with responsive width clamp
        var pnlContainer = new Panel
        {
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(24, 16, 24, 24)
        };

        // 1. Luxury Header Banner with Restaurant Emblem & Brand Identity
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 115,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(20, 14, 20, 14),
            Margin = new Padding(0, 0, 0, 18)
        };

        var picEmblem = new PictureBox
        {
            Size = new Size(86, 86),
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(18, 14),
            BackColor = Color.Transparent
        };

        var emblemPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "restaurant_emblem.png");
        if (File.Exists(emblemPath))
        {
            try { picEmblem.Image = Image.FromFile(emblemPath); } catch { }
        }

        var lblStars = new Label
        {
            Text = "★ ★ ★ ★ ★   PREMIUM RESORT & GASTRONOMIC COMPLEX   ★ ★ ★ ★ ★",
            Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
            ForeColor = Color.FromArgb(212, 175, 55), // Luxury Gold
            Location = new Point(118, 16),
            AutoSize = true
        };

        var lblWelcome = new Label
        {
            Text = "LUXERESTO  —  GRANDLUX  HORECA  OS",
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(116, 36),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Єдина автоматизована платформа: POS каса ресторану, KDS монітор шеф-кухаря, шахматка готелю, складські запаси та BI аналітика",
            Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(118, 74),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { picEmblem, lblStars, lblWelcome, lblSub });

        pnlHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemePalette.BorderLight, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlHeader.Width - 1, pnlHeader.Height - 1);
        };

        // Spacer below header
        var pnlSpacer1 = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };

        // 2. Quick Demo Cards Section (Generous spacing, zero text clipping)
        var pnlQuickDemo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 175,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0)
        };

        var lblDemoHeader = new Label
        {
            Text = "⚡ ШВИДКИЙ ТЕСТОВИЙ ВХІД В 1 КЛІК (Миттєвий перехід до відповідного модуля без введення паролю):",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Dock = DockStyle.Top,
            Height = 26
        };

        var tlpCards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        for (int i = 0; i < 5; i++)
        {
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        }

        tlpCards.Controls.Add(CreateDemoRoleCard("👑 Адміністратор", "admin / PIN: 1111", "BI Аналітика, фінанси, повний аудит", UserRole.Administrator, ThemePalette.Primary), 0, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("🍽️ Офіціант", "waiter / PIN: 2222", "Схема залів, замовлення, POS меню", UserRole.Waiter, ThemePalette.Success), 1, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("🛎️ Рецепція", "reception / PIN: 3333", "Шахматка номерів, гості, броні", UserRole.Receptionist, ThemePalette.Info), 2, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("👨‍🍳 Шеф-кухар", "chef / PIN: 4444", "KDS екран, таймери кухні, статуси", UserRole.Chef, ThemePalette.Warning), 3, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("📦 Склад", "store / PIN: 5555", "Залишки, надходження, дефіцит", UserRole.Storekeeper, Color.FromArgb(160, 170, 185)), 4, 0);

        pnlQuickDemo.Controls.Add(tlpCards);
        pnlQuickDemo.Controls.Add(lblDemoHeader);

        pnlQuickDemo.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemePalette.BorderLight, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlQuickDemo.Width - 1, pnlQuickDemo.Height - 1);
        };

        // Spacer below demo accounts
        var pnlSpacer2 = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };

        // 3. Two-Column Layout for Login & Registration (Well-proportioned, luxury styling)
        var pnlForms = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 445,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        pnlForms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
        pnlForms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));

        // LEFT: Login Card
        var pnlLoginCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(0, 0, 10, 0)
        };

        var lblLoginTitle = new Label
        {
            Text = "🔐  Авторизація працівника",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 32
        };

        var lblLoginHint = new Label
        {
            Text = "Введіть персональний логін або 4-значний PIN-код:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 26
        };

        var lblUser = new Label
        {
            Text = "Логін або PIN-код:",
            ForeColor = ThemePalette.TextSecondary,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 22,
            Margin = new Padding(0, 8, 0, 0)
        };

        _txtLoginUser = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11f)
        };

        var lblPass = new Label
        {
            Text = "Пароль (якщо входите за логіном):",
            ForeColor = ThemePalette.TextSecondary,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 26,
            Margin = new Padding(0, 14, 0, 0)
        };

        _txtLoginPass = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = '●',
            Font = new Font("Segoe UI", 11f)
        };

        var pnlLoginBtnSpacer = new Panel { Dock = DockStyle.Top, Height = 18 };

        _btnLogin = new Button
        {
            Text = "ВВІЙТИ В СИСТЕМУ  ➔",
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = ThemePalette.PrimaryDark,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnLogin.FlatAppearance.BorderColor = ThemePalette.Primary;
        _btnLogin.FlatAppearance.BorderSize = 1;
        _btnLogin.Click += async (s, e) => await ExecuteLoginAsync();

        _lblLoginStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var pnlLoginInner = new Panel { Dock = DockStyle.Fill };
        pnlLoginInner.Controls.Add(_lblLoginStatus);
        pnlLoginInner.Controls.Add(_btnLogin);
        pnlLoginInner.Controls.Add(pnlLoginBtnSpacer);
        pnlLoginInner.Controls.Add(_txtLoginPass);
        pnlLoginInner.Controls.Add(lblPass);
        pnlLoginInner.Controls.Add(_txtLoginUser);
        pnlLoginInner.Controls.Add(lblUser);

        pnlLoginCard.Controls.Add(pnlLoginInner);
        pnlLoginCard.Controls.Add(lblLoginHint);
        pnlLoginCard.Controls.Add(lblLoginTitle);

        pnlLoginCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemePalette.BorderLight, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlLoginCard.Width - 1, pnlLoginCard.Height - 1);
        };

        // RIGHT: Registration Card (Adaptive Grid)
        var pnlRegCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(26, 22, 26, 22),
            Margin = new Padding(10, 0, 0, 0)
        };

        var lblRegTitle = new Label
        {
            Text = "👤  Реєстрація нового працівника",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 32
        };

        var lblRegHint = new Label
        {
            Text = "Створіть власний обліковий запис для комплексу:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 26
        };

        var tlpRegGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Color.Transparent
        };
        tlpRegGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        tlpRegGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f)); // FIO & Username
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f)); // Password & PIN
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f)); // Role
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f)); // Button
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Status

        // Cell 0,0: FIO
        var pnlFio = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        var lblRegFio = new Label { Text = "ПІБ працівника:", ForeColor = ThemePalette.TextSecondary, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Dock = DockStyle.Top, Height = 20 };
        _txtRegFullName = new TextBox { Dock = DockStyle.Top, Height = 32, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };
        pnlFio.Controls.Add(_txtRegFullName);
        pnlFio.Controls.Add(lblRegFio);
        tlpRegGrid.Controls.Add(pnlFio, 0, 0);

        // Cell 1,0: Username
        var pnlUser = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        var lblRegLogin = new Label { Text = "Логін (Username):", ForeColor = ThemePalette.TextSecondary, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Dock = DockStyle.Top, Height = 20 };
        _txtRegUser = new TextBox { Dock = DockStyle.Top, Height = 32, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };
        pnlUser.Controls.Add(_txtRegUser);
        pnlUser.Controls.Add(lblRegLogin);
        tlpRegGrid.Controls.Add(pnlUser, 1, 0);

        // Cell 0,1: Password
        var pnlPass = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        var lblRegPassword = new Label { Text = "Пароль:", ForeColor = ThemePalette.TextSecondary, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Dock = DockStyle.Top, Height = 20 };
        _txtRegPass = new TextBox { Dock = DockStyle.Top, Height = 32, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, PasswordChar = '●', Font = ThemePalette.BodyFont };
        pnlPass.Controls.Add(_txtRegPass);
        pnlPass.Controls.Add(lblRegPassword);
        tlpRegGrid.Controls.Add(pnlPass, 0, 1);

        // Cell 1,1: PIN
        var pnlPin = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        var lblRegPin = new Label { Text = "PIN-код (4 цифри):", ForeColor = ThemePalette.TextSecondary, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Dock = DockStyle.Top, Height = 20 };
        _txtRegPin = new TextBox { Dock = DockStyle.Top, Height = 32, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, MaxLength = 4, Font = ThemePalette.BodyFont };
        pnlPin.Controls.Add(_txtRegPin);
        pnlPin.Controls.Add(lblRegPin);
        tlpRegGrid.Controls.Add(pnlPin, 1, 1);

        // Cell 0,2: Role (Span 2) - Custom Dark Themed Dropdown
        var pnlRole = new Panel { Dock = DockStyle.Fill };
        var lblRole = new Label { Text = "Посада та роль у системі комплексу:", ForeColor = ThemePalette.TextSecondary, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Dock = DockStyle.Top, Height = 20 };
        _cbRegRole = new ComboBox
        {
            Dock = DockStyle.Top,
            Height = 32,
            ItemHeight = 24,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed,
            FlatStyle = FlatStyle.Flat,
            Font = ThemePalette.BodyFont
        };
        _cbRegRole.Items.AddRange(new object[]
        {
            "Адміністратор комплексу (Administrator)",
            "Офіціант ресторану (Waiter)",
            "Адміністратор рецепції (Receptionist)",
            "Шеф-кухар / Кухня (Chef)",
            "Менеджер складу (Storekeeper)"
        });
        _cbRegRole.SelectedIndex = 1;

        _cbRegRole.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using var bgBrush = new SolidBrush(isSelected ? ThemePalette.PrimaryDark : ThemePalette.BackgroundInput);
            using var textBrush = new SolidBrush(isSelected ? Color.White : ThemePalette.TextPrimary);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);
            var itemText = _cbRegRole.Items[e.Index].ToString() ?? "";
            e.Graphics.DrawString(itemText, e.Font ?? ThemePalette.BodyFont, textBrush, e.Bounds.X + 4, e.Bounds.Y + 3);
            e.DrawFocusRectangle();
        };

        pnlRole.Controls.Add(_cbRegRole);
        pnlRole.Controls.Add(lblRole);
        tlpRegGrid.Controls.Add(pnlRole, 0, 2);
        tlpRegGrid.SetColumnSpan(pnlRole, 2);

        // Cell 0,3: Register Button (Span 2)
        _btnRegister = new Button
        {
            Text = "ЗАРЕЄСТРУВАТИ ТА ВВІЙТИ  ✓",
            Dock = DockStyle.Fill,
            Height = 46,
            BackColor = ThemePalette.Success,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnRegister.FlatAppearance.BorderSize = 0;
        _btnRegister.Click += async (s, e) => await ExecuteRegisterAsync();
        tlpRegGrid.Controls.Add(_btnRegister, 0, 3);
        tlpRegGrid.SetColumnSpan(_btnRegister, 2);

        // Cell 0,4: Status (Span 2)
        _lblRegStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            TextAlign = ContentAlignment.MiddleCenter
        };
        tlpRegGrid.Controls.Add(_lblRegStatus, 0, 4);
        tlpRegGrid.SetColumnSpan(_lblRegStatus, 2);

        pnlRegCard.Controls.Add(tlpRegGrid);
        pnlRegCard.Controls.Add(lblRegHint);
        pnlRegCard.Controls.Add(lblRegTitle);

        pnlRegCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemePalette.BorderLight, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlRegCard.Width - 1, pnlRegCard.Height - 1);
        };

        pnlForms.Controls.Add(pnlLoginCard, 0, 0);
        pnlForms.Controls.Add(pnlRegCard, 1, 0);

        // Assemble with proper top-down dock order
        pnlContainer.Controls.Add(pnlForms);
        pnlContainer.Controls.Add(pnlSpacer2);
        pnlContainer.Controls.Add(pnlQuickDemo);
        pnlContainer.Controls.Add(pnlSpacer1);
        pnlContainer.Controls.Add(pnlHeader);

        Controls.Add(pnlContainer);

        // Auto-center canvas container dynamically
        Action updateLayout = () =>
        {
            int targetWidth = Math.Min(1240, Math.Max(980, ClientSize.Width - 50));
            pnlContainer.Width = targetWidth;
            pnlContainer.Left = Math.Max(16, (ClientSize.Width - targetWidth) / 2);
            pnlContainer.Height = pnlHeader.Height + pnlSpacer1.Height + pnlQuickDemo.Height + pnlSpacer2.Height + pnlForms.Height + 50;
        };

        Resize += (s, e) => updateLayout();
        Load += (s, e) => updateLayout();
    }

    private Control CreateDemoRoleCard(string title, string creds, string desc, UserRole role, Color accentColor)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            Margin = new Padding(5),
            Padding = new Padding(12, 10, 12, 10),
            Cursor = Cursors.Hand
        };

        var lblCardTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 24,
            AutoEllipsis = true
        };

        var lblCreds = new Label
        {
            Text = creds,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = accentColor,
            Dock = DockStyle.Top,
            Height = 22,
            AutoEllipsis = true
        };

        var lblDesc = new Label
        {
            Text = desc,
            Font = new Font("Segoe UI", 8f),
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 34,
            AutoEllipsis = true
        };

        var lblHint = new Label
        {
            Text = "Вхід в 1 клік  ➔",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
            ForeColor = ThemePalette.TextSecondary,
            Dock = DockStyle.Bottom,
            Height = 16,
            TextAlign = ContentAlignment.MiddleRight
        };

        pnl.Controls.Add(lblHint);
        pnl.Controls.Add(lblDesc);
        pnl.Controls.Add(lblCreds);
        pnl.Controls.Add(lblCardTitle);

        bool isHovered = false;

        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(isHovered ? accentColor : ThemePalette.BorderLight, isHovered ? 1.5f : 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };

        Action onEnter = () =>
        {
            isHovered = true;
            pnl.BackColor = ThemePalette.BackgroundCardHover;
            lblHint.ForeColor = accentColor;
            pnl.Invalidate();
        };

        Action onLeave = () =>
        {
            isHovered = false;
            pnl.BackColor = ThemePalette.BackgroundDark;
            lblHint.ForeColor = ThemePalette.TextSecondary;
            pnl.Invalidate();
        };

        pnl.MouseEnter += (s, e) => onEnter();
        pnl.MouseLeave += (s, e) => onLeave();
        lblCardTitle.MouseEnter += (s, e) => onEnter();
        lblCreds.MouseEnter += (s, e) => onEnter();
        lblDesc.MouseEnter += (s, e) => onEnter();
        lblHint.MouseEnter += (s, e) => onEnter();

        Action clickAction = async () =>
        {
            _lblLoginStatus.ForeColor = ThemePalette.Warning;
            _lblLoginStatus.Text = $"Вхід як {title}...";
            var result = await _authService.QuickLoginAsync(role);
            if (result.Success && result.User != null)
            {
                UserLoggedIn?.Invoke(result.User);
            }
            else
            {
                _lblLoginStatus.ForeColor = ThemePalette.Danger;
                _lblLoginStatus.Text = result.ErrorMessage;
            }
        };

        pnl.Click += (s, e) => clickAction();
        lblCardTitle.Click += (s, e) => clickAction();
        lblCreds.Click += (s, e) => clickAction();
        lblDesc.Click += (s, e) => clickAction();
        lblHint.Click += (s, e) => clickAction();

        return pnl;
    }

    private async Task ExecuteLoginAsync()
    {
        _lblLoginStatus.Text = "";
        var login = _txtLoginUser.Text.Trim();
        var pass = _txtLoginPass.Text;

        if (string.IsNullOrWhiteSpace(login))
        {
            _lblLoginStatus.ForeColor = ThemePalette.Danger;
            _lblLoginStatus.Text = "Вкажіть логін або 4-значний PIN-код.";
            return;
        }

        _btnLogin.Enabled = false;
        try
        {
            var result = await _authService.LoginAsync(login, pass);
            if (result.Success && result.User != null)
            {
                UserLoggedIn?.Invoke(result.User);
            }
            else
            {
                _lblLoginStatus.ForeColor = ThemePalette.Danger;
                _lblLoginStatus.Text = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Помилка при вході в систему");
            _lblLoginStatus.ForeColor = ThemePalette.Danger;
            _lblLoginStatus.Text = $"Помилка: {ex.Message}";
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }

    private async Task ExecuteRegisterAsync()
    {
        _lblRegStatus.Text = "";

        var role = _cbRegRole.SelectedIndex switch
        {
            0 => UserRole.Administrator,
            1 => UserRole.Waiter,
            2 => UserRole.Receptionist,
            3 => UserRole.Chef,
            4 => UserRole.Storekeeper,
            _ => UserRole.Waiter
        };

        var request = new RegisterUserRequest(
            Username: _txtRegUser.Text.Trim(),
            FullName: _txtRegFullName.Text.Trim(),
            Password: _txtRegPass.Text,
            PinCode: _txtRegPin.Text.Trim(),
            Role: role
        );

        var val = await _registerValidator.ValidateAsync(request);
        if (!val.IsValid)
        {
            _lblRegStatus.ForeColor = ThemePalette.Danger;
            _lblRegStatus.Text = val.Errors[0].ErrorMessage;
            return;
        }

        _btnRegister.Enabled = false;
        try
        {
            var result = await _authService.RegisterAsync(request);
            if (result.Success && result.User != null)
            {
                UserLoggedIn?.Invoke(result.User);
            }
            else
            {
                _lblRegStatus.ForeColor = ThemePalette.Danger;
                _lblRegStatus.Text = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Помилка при реєстрації");
            _lblRegStatus.ForeColor = ThemePalette.Danger;
            _lblRegStatus.Text = $"Помилка: {ex.Message}";
        }
        finally
        {
            _btnRegister.Enabled = true;
        }
    }
}
