using System.Drawing;
using System.Windows.Forms;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Application.Validators;
using HoReCa.Domain.Enums;
using HoReCa.WinForms.Common;
using Serilog;

namespace HoReCa.WinForms.Forms;

public class LoginForm : BaseForm
{
    private readonly IAuthService _authService;
    private readonly RegisterUserRequestValidator _registerValidator = new();

    // Mode switch
    private bool _isRegisterMode = false;
    private readonly Panel _pnlCard;
    private readonly Label _lblTitle;
    private readonly Label _lblSubtitle;
    private readonly Button _btnToggleMode;

    // Login inputs
    private readonly Panel _pnlLogin;
    private readonly TextBox _txtLoginUsername;
    private readonly TextBox _txtLoginPassword;
    private readonly Button _btnLogin;

    // Register inputs
    private readonly Panel _pnlRegister;
    private readonly TextBox _txtRegFullName;
    private readonly TextBox _txtRegUsername;
    private readonly TextBox _txtRegPassword;
    private readonly TextBox _txtRegPin;
    private readonly ComboBox _cbRegRole;
    private readonly Button _btnRegister;

    // Status label
    private readonly Label _lblStatus;

    public bool IsAuthenticated { get; private set; }

    public LoginForm(IAuthService authService)
    {
        _authService = authService;

        Text = "LuxeResto HoReCa OS — Авторизація та Швидкий Демо-Доступ";
        Size = new Size(980, 680);
        MinimumSize = new Size(950, 640);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        // Header Banner
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = ThemePalette.BackgroundSidebar,
            Padding = new Padding(25, 12, 25, 12)
        };

        var lblBrand = new Label
        {
            Text = "LUXERESTO  HoReCa  OS",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ThemePalette.Primary,
            Location = new Point(25, 15),
            AutoSize = true
        };

        var lblBrandDesc = new Label
        {
            Text = "Система комплексного управління готелем, рестораном та кухнею  |  .NET 10 LTS",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(26, 48),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblBrand, lblBrandDesc });

        // Main background container
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.BackgroundDark,
            Padding = new Padding(30, 20, 30, 20)
        };

        // Quick Demo Access Section (Top)
        var pnlDemoSection = new Panel
        {
            Dock = DockStyle.Top,
            Height = 145,
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(15)
        };

        var lblDemoTitle = new Label
        {
            Text = "⚡ ШВИДКИЙ ТЕСТОВИЙ ВХІД (1 клік для миттєвого ознайомлення з ролями):",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ThemePalette.Warning,
            Location = new Point(15, 12),
            AutoSize = true
        };

        var flpDemoButtons = new FlowLayoutPanel
        {
            Location = new Point(15, 40),
            Size = new Size(890, 90),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false
        };

        flpDemoButtons.Controls.Add(CreateDemoCard("👑 Адміністратор", "admin / PIN: 1111", "Повний доступ, фінанси, BI", UserRole.Administrator, ThemePalette.Primary));
        flpDemoButtons.Controls.Add(CreateDemoCard("🍽️ Офіціант", "waiter / PIN: 2222", "Столики, замовлення, POS", UserRole.Waiter, ThemePalette.Success));
        flpDemoButtons.Controls.Add(CreateDemoCard("🏨 Рецепція", "reception / PIN: 3333", "Шахматка, гості, бронь", UserRole.Receptionist, ThemePalette.Info));
        flpDemoButtons.Controls.Add(CreateDemoCard("👨‍🍳 Шеф-кухар", "chef / PIN: 4444", "KDS екран, таймери кухні", UserRole.Chef, ThemePalette.Warning));
        flpDemoButtons.Controls.Add(CreateDemoCard("📦 Склад", "store / PIN: 5555", "Товари, залишки, дефіцит", UserRole.Storekeeper, Color.FromArgb(236, 72, 153)));

        pnlDemoSection.Controls.AddRange(new Control[] { lblDemoTitle, flpDemoButtons });

        // Center Card: Login / Register Form
        _pnlCard = new Panel
        {
            Location = new Point(180, 185),
            Size = new Size(580, 420),
            BackColor = ThemePalette.BackgroundCard,
            Padding = new Padding(25)
        };

        _lblTitle = new Label
        {
            Text = "Вхід до особистого кабінету",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ThemePalette.TextPrimary,
            Location = new Point(25, 20),
            AutoSize = true
        };

        _lblSubtitle = new Label
        {
            Text = "Введіть логін та пароль або 4-значний PIN-код працівника:",
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(26, 48),
            AutoSize = true
        };

        _btnToggleMode = new Button
        {
            Text = "Немає акаунту? Зареєструватися ➔",
            Location = new Point(320, 20),
            Size = new Size(230, 28),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ThemePalette.Primary,
            BackColor = Color.Transparent,
            Font = ThemePalette.SmallFont,
            Cursor = Cursors.Hand
        };
        _btnToggleMode.FlatAppearance.BorderSize = 0;
        _btnToggleMode.Click += (s, e) => ToggleMode();

        // Status / Error message
        _lblStatus = new Label
        {
            Location = new Point(25, 365),
            Size = new Size(530, 35),
            ForeColor = ThemePalette.Danger,
            Font = ThemePalette.SmallFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // 1. Login Panel
        _pnlLogin = new Panel
        {
            Location = new Point(25, 80),
            Size = new Size(530, 270),
            BackColor = Color.Transparent
        };

        var lblLoginUser = new Label { Text = "Логін або PIN-код:", Location = new Point(0, 15), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtLoginUsername = new TextBox
        {
            Location = new Point(0, 38),
            Size = new Size(530, 34),
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12f)
        };

        var lblLoginPass = new Label { Text = "Пароль (не потрібен, якщо введено PIN):", Location = new Point(0, 85), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtLoginPassword = new TextBox
        {
            Location = new Point(0, 108),
            Size = new Size(530, 34),
            BackColor = ThemePalette.BackgroundInput,
            ForeColor = ThemePalette.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = '●',
            Font = new Font("Segoe UI", 12f)
        };

        _btnLogin = new Button
        {
            Text = "УВІЙТИ В СИСТЕМУ",
            Location = new Point(0, 175),
            Size = new Size(530, 48),
            BackColor = ThemePalette.Primary,
            ForeColor = ThemePalette.TextInverse,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.Click += async (s, e) => await ExecuteLoginAsync();

        _pnlLogin.Controls.AddRange(new Control[] { lblLoginUser, _txtLoginUsername, lblLoginPass, _txtLoginPassword, _btnLogin });

        // 2. Register Panel
        _pnlRegister = new Panel
        {
            Location = new Point(25, 80),
            Size = new Size(530, 280),
            BackColor = Color.Transparent,
            Visible = false
        };

        var lblRegFullName = new Label { Text = "ПІБ Працівника:", Location = new Point(0, 5), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegFullName = new TextBox { Location = new Point(0, 24), Size = new Size(255, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };

        var lblRegUser = new Label { Text = "Логін (Username):", Location = new Point(275, 5), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegUsername = new TextBox { Location = new Point(275, 24), Size = new Size(255, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = ThemePalette.BodyFont };

        var lblRegPass = new Label { Text = "Пароль:", Location = new Point(0, 65), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegPassword = new TextBox { Location = new Point(0, 84), Size = new Size(255, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, PasswordChar = '●', Font = ThemePalette.BodyFont };

        var lblRegPin = new Label { Text = "PIN-код (4 цифри):", Location = new Point(275, 65), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _txtRegPin = new TextBox { Location = new Point(275, 84), Size = new Size(255, 30), BackColor = ThemePalette.BackgroundInput, ForeColor = ThemePalette.TextPrimary, BorderStyle = BorderStyle.FixedSingle, MaxLength = 4, Font = ThemePalette.BodyFont };

        var lblRegRole = new Label { Text = "Посада / Роль у закладі:", Location = new Point(0, 125), ForeColor = ThemePalette.TextMuted, Font = ThemePalette.SmallFont, AutoSize = true };
        _cbRegRole = new ComboBox
        {
            Location = new Point(0, 144),
            Size = new Size(530, 32),
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
            Location = new Point(0, 195),
            Size = new Size(530, 48),
            BackColor = ThemePalette.Success,
            ForeColor = ThemePalette.TextInverse,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnRegister.FlatAppearance.BorderSize = 0;
        _btnRegister.Click += async (s, e) => await ExecuteRegisterAsync();

        _pnlRegister.Controls.AddRange(new Control[]
        {
            lblRegFullName, _txtRegFullName,
            lblRegUser, _txtRegUsername,
            lblRegPass, _txtRegPassword,
            lblRegPin, _txtRegPin,
            lblRegRole, _cbRegRole,
            _btnRegister
        });

        _pnlCard.Controls.AddRange(new Control[] { _lblTitle, _lblSubtitle, _btnToggleMode, _pnlLogin, _pnlRegister, _lblStatus });

        pnlMain.Controls.AddRange(new Control[] { pnlDemoSection, _pnlCard });

        Controls.AddRange(new Control[] { pnlMain, pnlHeader });

        // Enable Enter key submission
        AcceptButton = _btnLogin;
    }

    private Control CreateDemoCard(string title, string credentials, string desc, UserRole role, Color accentColor)
    {
        var btn = new Button
        {
            Size = new Size(170, 78),
            BackColor = ThemePalette.BackgroundDark,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ThemePalette.Border;
        btn.FlatAppearance.MouseOverBackColor = ThemePalette.BackgroundInput;

        var lblCardTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(6, 6),
            AutoSize = true
        };

        var lblCreds = new Label
        {
            Text = credentials,
            Font = ThemePalette.SmallFont,
            ForeColor = ThemePalette.TextPrimary,
            Location = new Point(6, 28),
            AutoSize = true
        };

        var lblDesc = new Label
        {
            Text = desc,
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = ThemePalette.TextMuted,
            Location = new Point(6, 50),
            AutoSize = true
        };

        btn.Controls.AddRange(new Control[] { lblCardTitle, lblCreds, lblDesc });

        // Click handler on button and labels
        Action clickAction = async () =>
        {
            _lblStatus.ForeColor = ThemePalette.Warning;
            _lblStatus.Text = $"Вхід як {title}...";
            var result = await _authService.QuickLoginAsync(role);
            if (result.Success)
            {
                IsAuthenticated = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.ForeColor = ThemePalette.Danger;
                _lblStatus.Text = result.ErrorMessage;
            }
        };

        btn.Click += (s, e) => clickAction();
        lblCardTitle.Click += (s, e) => clickAction();
        lblCreds.Click += (s, e) => clickAction();
        lblDesc.Click += (s, e) => clickAction();

        return btn;
    }

    private void ToggleMode()
    {
        _isRegisterMode = !_isRegisterMode;
        _lblStatus.Text = "";

        if (_isRegisterMode)
        {
            _lblTitle.Text = "Реєстрація нового працівника";
            _lblSubtitle.Text = "Заповніть форму для створення облікового запису:";
            _btnToggleMode.Text = "➔ Вже є акаунт? Увійти";
            _pnlLogin.Visible = false;
            _pnlRegister.Visible = true;
            AcceptButton = _btnRegister;
        }
        else
        {
            _lblTitle.Text = "Вхід до особистого кабінету";
            _lblSubtitle.Text = "Введіть логін та пароль або 4-значний PIN-код працівника:";
            _btnToggleMode.Text = "Немає акаунту? Зареєструватися ➔";
            _pnlLogin.Visible = true;
            _pnlRegister.Visible = false;
            AcceptButton = _btnLogin;
        }
    }

    private async Task ExecuteLoginAsync()
    {
        _lblStatus.Text = "";
        var login = _txtLoginUsername.Text.Trim();
        var pass = _txtLoginPassword.Text;

        if (string.IsNullOrWhiteSpace(login))
        {
            _lblStatus.ForeColor = ThemePalette.Danger;
            _lblStatus.Text = "Будь ласка, вкажіть логін або 4-значний PIN-код.";
            return;
        }

        _btnLogin.Enabled = false;
        try
        {
            var result = await _authService.LoginAsync(login, pass);
            if (result.Success)
            {
                IsAuthenticated = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.ForeColor = ThemePalette.Danger;
                _lblStatus.Text = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Помилка при вході в систему");
            _lblStatus.ForeColor = ThemePalette.Danger;
            _lblStatus.Text = $"Помилка з'єднання: {ex.Message}";
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }

    private async Task ExecuteRegisterAsync()
    {
        _lblStatus.Text = "";

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
            Username: _txtRegUsername.Text.Trim(),
            FullName: _txtRegFullName.Text.Trim(),
            Password: _txtRegPassword.Text,
            PinCode: _txtRegPin.Text.Trim(),
            Role: role
        );

        var val = await _registerValidator.ValidateAsync(request);
        if (!val.IsValid)
        {
            _lblStatus.ForeColor = ThemePalette.Danger;
            _lblStatus.Text = val.Errors[0].ErrorMessage;
            return;
        }

        _btnRegister.Enabled = false;
        try
        {
            var result = await _authService.RegisterAsync(request);
            if (result.Success)
            {
                IsAuthenticated = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.ForeColor = ThemePalette.Danger;
                _lblStatus.Text = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Помилка при реєстрації");
            _lblStatus.ForeColor = ThemePalette.Danger;
            _lblStatus.Text = $"Помилка реєстрації: {ex.Message}";
        }
        finally
        {
            _btnRegister.Enabled = true;
        }
    }
}
