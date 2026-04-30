using System.Drawing;
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

        // Main scrollable canvas container
        var pnlContainer = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(25, 20, 25, 30),
            BackColor = Color.Transparent
        };

        // 1. Header Banner
        var pnlHeader = new Panel
        {
            Height = 85,
            Dock = DockStyle.Top,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(20, 15, 20, 15),
            Margin = new Padding(0, 0, 0, 15)
        };

        var lblWelcome = new Label
        {
            Text = "ВІТАЄМО В СИСТЕМІ  LUXERESTO  HoReCa  OS",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Location = new Point(20, 14),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Оберіть демо-роль нижче для миттєвого ознайомлення в 1 клік, увійдіть за обліковими даними або зареєструйтесь.",
            Font = ThemePalette.BodyFont,
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(22, 48),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblWelcome, lblSub });

        // 2. Quick Demo Cards Section
        var pnlQuickDemo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 150,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(15, 12, 15, 12),
            Margin = new Padding(0, 15, 0, 15)
        };

        var lblDemoHeader = new Label
        {
            Text = "⚡ ШВИДКИЙ ТЕСТОВИЙ ВХІД В 1 КЛІК (Миттєвий перехід до відповідного модуля):",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Dock = DockStyle.Top,
            Height = 24
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

        tlpCards.Controls.Add(CreateDemoRoleCard("👑 Адміністратор", "admin / PIN: 1111", "BI Аналітика, повний аудит", UserRole.Administrator, ThemePalette.Primary), 0, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("🍽️ Офіціант", "waiter / PIN: 2222", "Столики, замовлення, POS", UserRole.Waiter, ThemePalette.Success), 1, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("🏨 Рецепція", "reception / PIN: 3333", "Шахматка, гості, бронь", UserRole.Receptionist, ThemePalette.Info), 2, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("👨‍🍳 Шеф-кухар", "chef / PIN: 4444", "KDS екран, таймери кухні", UserRole.Chef, ThemePalette.Warning), 3, 0);
        tlpCards.Controls.Add(CreateDemoRoleCard("📦 Склад", "store / PIN: 5555", "Товари, залишки, дефіцит", UserRole.Storekeeper, ThemePalette.TextMuted), 4, 0);

        pnlQuickDemo.Controls.Add(tlpCards);
        pnlQuickDemo.Controls.Add(lblDemoHeader);

        // 3. Two-Column Layout for Login & Registration (Fully Responsive)
        var pnlForms = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 440,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 15, 0, 0)
        };
        pnlForms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        pnlForms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

        // LEFT: Login Card
        var pnlLoginCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(24),
            Margin = new Padding(0, 0, 10, 0)
        };

        var lblLoginTitle = new Label
        {
            Text = "Вхід до системи",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        var lblLoginHint = new Label
        {
            Text = "Введіть логін або 4-значний PIN-код:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 24
        };

        var lblUser = new Label { Text = "Логін або PIN:", ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, Dock = DockStyle.Top, Height = 22, Margin = new Padding(0, 10, 0, 0) };
        _txtLoginUser = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11f)
        };

        var lblPass = new Label { Text = "Пароль (якщо входите за логіном):", ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, Dock = DockStyle.Top, Height = 26, Margin = new Padding(0, 12, 0, 0) };
        _txtLoginPass = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = '●',
            Font = new Font("Segoe UI", 11f)
        };

        _btnLogin = new Button
        {
            Text = "УВІЙТИ В СИСТЕМУ",
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = ThemePalette.PrimaryDark,
            ForeColor = ThemePalette.TextPrimary,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 20, 0, 0)
        };
        _btnLogin.FlatAppearance.BorderColor = ThemePalette.BorderLight;
        _btnLogin.Click += async (s, e) => await ExecuteLoginAsync();

        _lblLoginStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 45,
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            Margin = new Padding(0, 8, 0, 0)
        };

        var pnlLoginInner = new Panel { Dock = DockStyle.Fill };
        pnlLoginInner.Controls.Add(_lblLoginStatus);
        pnlLoginInner.Controls.Add(_btnLogin);
        pnlLoginInner.Controls.Add(_txtLoginPass);
        pnlLoginInner.Controls.Add(lblPass);
        pnlLoginInner.Controls.Add(_txtLoginUser);
        pnlLoginInner.Controls.Add(lblUser);

        pnlLoginCard.Controls.Add(pnlLoginInner);
        pnlLoginCard.Controls.Add(lblLoginHint);
        pnlLoginCard.Controls.Add(lblLoginTitle);

        // RIGHT: Registration Card (Adaptive Grid)
        var pnlRegCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(24),
            Margin = new Padding(10, 0, 0, 0)
        };

        var lblRegTitle = new Label
        {
            Text = "Реєстрація нового працівника",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        var lblRegHint = new Label
        {
            Text = "Створіть власний обліковий запис працівника комплексу:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Top,
            Height = 24
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
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f)); // FIO & Username
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f)); // Password & PIN
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f)); // Role
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Button
        tlpRegGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Status

        // Cell 0,0: FIO
        var pnlFio = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        var lblRegFio = new Label { Text = "ПІБ:", ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, Dock = DockStyle.Top, Height = 18 };
        _txtRegFullName = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };
        pnlFio.Controls.Add(_txtRegFullName);
        pnlFio.Controls.Add(lblRegFio);
        tlpRegGrid.Controls.Add(pnlFio, 0, 0);

        // Cell 1,0: Username
        var pnlUser = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        var lblRegLogin = new Label { Text = "Логін (Username):", ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, Dock = DockStyle.Top, Height = 18 };
        _txtRegUser = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };
        pnlUser.Controls.Add(_txtRegUser);
        pnlUser.Controls.Add(lblRegLogin);
        tlpRegGrid.Controls.Add(pnlUser, 1, 0);

        // Cell 0,1: Password
        var pnlPass = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        var lblRegPassword = new Label { Text = "Пароль:", ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, Dock = DockStyle.Top, Height = 18 };
        _txtRegPass = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, PasswordChar = '●', Font = ThemePalette.BodyFont };
        pnlPass.Controls.Add(_txtRegPass);
        pnlPass.Controls.Add(lblRegPassword);
        tlpRegGrid.Controls.Add(pnlPass, 0, 1);

        // Cell 1,1: PIN
        var pnlPin = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        var lblRegPin = new Label { Text = "PIN (4 цифри):", ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, Dock = DockStyle.Top, Height = 18 };
        _txtRegPin = new TextBox { Dock = DockStyle.Top, Height = 30, BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, MaxLength = 4, Font = ThemePalette.BodyFont };
        pnlPin.Controls.Add(_txtRegPin);
        pnlPin.Controls.Add(lblRegPin);
        tlpRegGrid.Controls.Add(pnlPin, 1, 1);

        // Cell 0,2: Role (Span 2)
        var pnlRole = new Panel { Dock = DockStyle.Fill };
        var lblRole = new Label { Text = "Посада / Роль:", ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, Dock = DockStyle.Top, Height = 18 };
        _cbRegRole = new ComboBox
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = ThemePalette.BodyFont
        };
        _cbRegRole.Items.AddRange(new object[]
        {
            "Адміністратор (Administrator)",
            "Офіціант ресторану (Waiter)",
            "Адміністратор рецепції (Receptionist)",
            "Шеф-кухар (Chef)",
            "Менеджер складу (Storekeeper)"
        });
        _cbRegRole.SelectedIndex = 1;
        pnlRole.Controls.Add(_cbRegRole);
        pnlRole.Controls.Add(lblRole);
        tlpRegGrid.Controls.Add(pnlRole, 0, 2);
        tlpRegGrid.SetColumnSpan(pnlRole, 2);

        // Cell 0,3: Register Button (Span 2)
        _btnRegister = new Button
        {
            Text = "ЗАРЕЄСТРУВАТИ ТА УВІЙТИ",
            Dock = DockStyle.Fill,
            Height = 44,
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextPrimary,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
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
            Font = ThemePalette.SmallFont
        };
        tlpRegGrid.Controls.Add(_lblRegStatus, 0, 4);
        tlpRegGrid.SetColumnSpan(_lblRegStatus, 2);

        pnlRegCard.Controls.Add(tlpRegGrid);
        pnlRegCard.Controls.Add(lblRegHint);
        pnlRegCard.Controls.Add(lblRegTitle);

        pnlForms.Controls.Add(pnlLoginCard, 0, 0);
        pnlForms.Controls.Add(pnlRegCard, 1, 0);

        // Add sections to main canvas
        pnlContainer.Controls.Add(pnlForms);
        pnlContainer.Controls.Add(pnlQuickDemo);
        pnlContainer.Controls.Add(pnlHeader);

        Controls.Add(pnlContainer);
    }

    private Control CreateDemoRoleCard(string title, string creds, string desc, UserRole role, Color accentColor)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            Margin = new Padding(4),
            Padding = new Padding(10, 8, 10, 8),
            Cursor = Cursors.Hand
        };

        var lblCardTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Dock = DockStyle.Top,
            Height = 22,
            AutoEllipsis = true
        };

        var lblCreds = new Label
        {
            Text = creds,
            Font = ThemePalette.SmallFont,
            ForeColor = accentColor,
            Dock = DockStyle.Top,
            Height = 20,
            AutoEllipsis = true
        };

        var lblDesc = new Label
        {
            Text = desc,
            Font = new Font("Segoe UI", 8f),
            ForeColor = ThemePalette.TextMuted,
            Dock = DockStyle.Fill,
            AutoEllipsis = true
        };

        pnl.Controls.Add(lblDesc);
        pnl.Controls.Add(lblCreds);
        pnl.Controls.Add(lblCardTitle);

        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemePalette.BorderLight, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };

        pnl.MouseEnter += (s, e) => pnl.BackColor = ThemePalette.BackgroundCardHover;
        pnl.MouseLeave += (s, e) => pnl.BackColor = ThemePalette.BackgroundDark;
        lblCardTitle.MouseEnter += (s, e) => pnl.BackColor = ThemePalette.BackgroundCardHover;
        lblCreds.MouseEnter += (s, e) => pnl.BackColor = ThemePalette.BackgroundCardHover;
        lblDesc.MouseEnter += (s, e) => pnl.BackColor = ThemePalette.BackgroundCardHover;

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
