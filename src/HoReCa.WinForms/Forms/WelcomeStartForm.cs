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
            Height = 160,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(15),
            Margin = new Padding(0, 15, 0, 15)
        };

        var lblDemoHeader = new Label
        {
            Text = "⚡ ШВИДКИЙ ТЕСТОВИЙ ВХІД В 1 КЛІК (Миттєвий перехід до відповідного модуля):",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ThemePalette.Warning,
            Location = new Point(15, 12),
            AutoSize = true
        };

        var flpCards = new FlowLayoutPanel
        {
            Location = new Point(15, 42),
            Size = new Size(1000, 105),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false
        };

        flpCards.Controls.Add(CreateDemoRoleCard("👑 Адміністратор", "admin / PIN: 1111", "BI Аналітика, повний аудит", UserRole.Administrator, ThemePalette.Primary));
        flpCards.Controls.Add(CreateDemoRoleCard("🍽️ Офіціант", "waiter / PIN: 2222", "Столики, замовлення, POS", UserRole.Waiter, ThemePalette.Success));
        flpCards.Controls.Add(CreateDemoRoleCard("🏨 Рецепція", "reception / PIN: 3333", "Шахматка, гості, бронь", UserRole.Receptionist, ThemePalette.Info));
        flpCards.Controls.Add(CreateDemoRoleCard("👨‍🍳 Шеф-кухар", "chef / PIN: 4444", "KDS екран, таймери кухні", UserRole.Chef, ThemePalette.Warning));
        flpCards.Controls.Add(CreateDemoRoleCard("📦 Склад", "store / PIN: 5555", "Товари, залишки, дефіцит", UserRole.Storekeeper, Color.FromArgb(236, 72, 153)));

        pnlQuickDemo.Controls.AddRange(new Control[] { lblDemoHeader, flpCards });

        // 3. Two-Column Layout for Login & Registration
        var pnlForms = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 400,
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
            Padding = new Padding(25),
            Margin = new Padding(0, 0, 10, 0)
        };

        var lblLoginTitle = new Label
        {
            Text = "Вхід до системи",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Location = new Point(25, 20),
            AutoSize = true
        };

        var lblLoginHint = new Label
        {
            Text = "Введіть логін або 4-значний PIN-код:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(26, 48),
            AutoSize = true
        };

        var lblUser = new Label { Text = "Логін або PIN:", Location = new Point(25, 80), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtLoginUser = new TextBox
        {
            Location = new Point(25, 102),
            Size = new Size(380, 32),
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11f)
        };

        var lblPass = new Label { Text = "Пароль (якщо входите за логіном):", Location = new Point(25, 145), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtLoginPass = new TextBox
        {
            Location = new Point(25, 167),
            Size = new Size(380, 32),
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = '●',
            Font = new Font("Segoe UI", 11f)
        };

        _btnLogin = new Button
        {
            Text = "УВІЙТИ В СИСТЕМУ",
            Location = new Point(25, 225),
            Size = new Size(380, 44),
            BackColor = ThemePalette.Primary,
            ForeColor = ThemePalette.TextInverse,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.Click += async (s, e) => await ExecuteLoginAsync();

        _lblLoginStatus = new Label
        {
            Location = new Point(25, 285),
            Size = new Size(380, 45),
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont
        };

        pnlLoginCard.Controls.AddRange(new Control[] { lblLoginTitle, lblLoginHint, lblUser, _txtLoginUser, lblPass, _txtLoginPass, _btnLogin, _lblLoginStatus });

        // RIGHT: Registration Card
        var pnlRegCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(25),
            Margin = new Padding(10, 0, 0, 0)
        };

        var lblRegTitle = new Label
        {
            Text = "Реєстрація нового працівника",
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Location = new Point(25, 20),
            AutoSize = true
        };

        var lblRegHint = new Label
        {
            Text = "Створіть власний обліковий запис працівника комплексу:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(26, 48),
            AutoSize = true
        };

        var lblRegFio = new Label { Text = "ПІБ:", Location = new Point(25, 80), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegFullName = new TextBox { Location = new Point(25, 102), Size = new Size(200, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };

        var lblRegLogin = new Label { Text = "Логін (Username):", Location = new Point(240, 80), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegUser = new TextBox { Location = new Point(240, 102), Size = new Size(190, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };

        var lblRegPassword = new Label { Text = "Пароль:", Location = new Point(25, 145), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegPass = new TextBox { Location = new Point(25, 167), Size = new Size(200, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, PasswordChar = '●', Font = ThemePalette.BodyFont };

        var lblRegPin = new Label { Text = "PIN (4 цифри):", Location = new Point(240, 145), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegPin = new TextBox { Location = new Point(240, 167), Size = new Size(190, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, MaxLength = 4, Font = ThemePalette.BodyFont };

        var lblRole = new Label { Text = "Посада / Роль:", Location = new Point(25, 205), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _cbRegRole = new ComboBox
        {
            Location = new Point(25, 227),
            Size = new Size(405, 30),
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

        _btnRegister = new Button
        {
            Text = "ЗАРЕЄСТРУВАТИ ТА УВІЙТИ",
            Location = new Point(25, 275),
            Size = new Size(405, 44),
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnRegister.FlatAppearance.BorderSize = 0;
        _btnRegister.Click += async (s, e) => await ExecuteRegisterAsync();

        _lblRegStatus = new Label
        {
            Location = new Point(25, 330),
            Size = new Size(405, 45),
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont
        };

        pnlRegCard.Controls.AddRange(new Control[]
        {
            lblRegTitle, lblRegHint,
            lblRegFio, _txtRegFullName,
            lblRegLogin, _txtRegUser,
            lblRegPassword, _txtRegPass,
            lblRegPin, _txtRegPin,
            lblRole, _cbRegRole,
            _btnRegister, _lblRegStatus
        });

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
        var btn = new Button
        {
            Size = new Size(185, 90),
            BackColor = ThemePalette.BackgroundDark,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ThemePalette.Border;
        btn.FlatAppearance.MouseOverBackColor = ThemePalette.BackgroundInput;

        var lblCardTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(8, 8),
            AutoSize = true
        };

        var lblCreds = new Label
        {
            Text = creds,
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextPrimary,
            Location = new Point(8, 32),
            AutoSize = true
        };

        var lblDesc = new Label
        {
            Text = desc,
            Font = new Font("Segoe UI", 7.8f),
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(8, 56),
            AutoSize = true
        };

        btn.Controls.AddRange(new Control[] { lblCardTitle, lblCreds, lblDesc });

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

        btn.Click += (s, e) => clickAction();
        lblCardTitle.Click += (s, e) => clickAction();
        lblCreds.Click += (s, e) => clickAction();
        lblDesc.Click += (s, e) => clickAction();

        return btn;
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
