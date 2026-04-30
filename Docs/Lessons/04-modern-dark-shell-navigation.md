# Практичний гайд 04: Сучасна навігаційна оболонка (Main Shell Form) та нейтральна темна палітра

## 1. Теоретична база та дизайн-система

Працівники індустрії гостинності (офіціанти, портьє, кухарі) працюють за моніторами позмінно від 8 до 14 годин поспіль. Класичні білі інтерфейси викликають швидку втому очей, тоді як чисто чорний `#000000` створює надмірний токсичний контраст (*halation effect*).

Дизайн-система **LuxeResto** базується на **нейтральній графітовій палітрі (Neutral Charcoal Theme)**:
- Фон полотна: `#121216` (глибокий графіт).
- Контейнери карток: `#1C1C22` (м'який антрацит).
- Семантичні статуси: м'який шавлієвий зелений (`#449B6E`), теплий бурштиновий (`#CD9637`), цегляний червоний (`#CD4B4B`).
- Архітектура інтерфейсу побудована за шаблоном **Single Shell Architecture**: одне головне вікно керує боковим меню, статус-баром та динамічно змінює робочі представлення без перевідкриття вікон.

---

## 2. Архітектурна схема навігації

```mermaid
sequenceDiagram
    autonumber
    actor User as Користувач
    participant Shell as MainShellForm
    participant Nav as Панель навігації (Sidebar)
    participant Content as Контейнер контенту (_pnlContent)
    participant DI as IServiceProvider
    participant Form as Вибрана форма (напр. PosTerminalForm)

    User->>Nav: Клік по кнопці "🍽️ Ресторан & POS"
    Nav->>Shell: OnNavButtonClicked(Button)
    Shell->>Shell: Підсвічування активної кнопки (ThemePalette.BackgroundCard)
    Shell->>Content: Controls.Clear()
    Shell->>DI: GetRequiredService<PosTerminalForm>()
    DI-->>Shell: Екземпляр форми
    Shell->>Form: TopLevel = false; FormBorderStyle = None; Dock = Fill
    Shell->>Content: Controls.Add(Form)
    Shell->>Form: Show()
    Shell->>Shell: Оновлення рядка стану (Статус БД, Користувач, Час)
```

---

## 3. Детальний розбір реалізації в проєкті

### 3.1. Централізована палітра: [`ThemePalette.cs`](file:///C:/Users/Yurii/source/repos/- MASTERY/WinForms-HoReCa-Mastery/src/HoReCa.WinForms/Common/ThemePalette.cs)

Усі візуальні компоненти підключають кольори виключно через клас [`ThemePalette`](file:///C:/Users/Yurii/source/repos/- MASTERY/WinForms-HoReCa-Mastery/src/HoReCa.WinForms/Common/ThemePalette.cs):

```csharp
public static class ThemePalette
{
    public static readonly Color BackgroundDark = Color.FromArgb(18, 18, 22);
    public static readonly Color BackgroundCard = Color.FromArgb(28, 28, 34);
    public static readonly Color BackgroundSidebar = Color.FromArgb(13, 13, 16);
    public static readonly Color BackgroundHeader = Color.FromArgb(22, 22, 26);

    public static readonly Color Primary = Color.FromArgb(150, 175, 205);
    public static readonly Color PrimaryDark = Color.FromArgb(55, 68, 88);

    public static readonly Color Success = Color.FromArgb(68, 155, 110);
    public static readonly Color Warning = Color.FromArgb(205, 150, 55);
    public static readonly Color Danger = Color.FromArgb(205, 75, 75);
    public static readonly Color Info = Color.FromArgb(100, 135, 180);

    public static readonly Color TextPrimary = Color.FromArgb(232, 235, 240);
    public static readonly Color TextSecondary = Color.FromArgb(180, 186, 200);
    public static readonly Color TextMuted = Color.FromArgb(140, 145, 158);
}
```

### 3.2. Головна навігаційна форма: [`MainShellForm.cs`](file:///C:/Users/Yurii/source/repos/- MASTERY/WinForms-HoReCa-Mastery/src/HoReCa.WinForms/Forms/MainShellForm.cs)

Вбудовування внутрішніх форм здійснюється за допомогою методу `ShowView<T>()`:
```csharp
public void ShowView<T>() where T : Form
{
    _pnlContent.Controls.Clear();
    var form = _serviceProvider.GetRequiredService<T>();
    form.TopLevel = false;
    form.FormBorderStyle = FormBorderStyle.None;
    form.Dock = DockStyle.Fill;
    _pnlContent.Controls.Add(form);
    form.Show();
}
```

---

## 4. Підводні камені та типові помилки

> [!WARNING]
> **Утилізація пам'яті при зміні представлень:**
> Якщо викликати `_pnlContent.Controls.Clear()` без коректного завершення життєвого циклу попередньої форми, її внутрішні підписки на події (таймери, шини повідомлень) залишаться в пам'яті, що призведе до поступового витоку RAM.
> **Рішення:** перед очищенням контейнера перевіряйте, чи форма реалізує `IDisposable` або відписуйтеся від подій у її методі `OnFormClosing`.

---

## 5. Практичний чек-лист для закріплення
1. [ ] Перевірити, що розміри форми адаптуються від 1100x700 до 4K без зрізання тексту.
2. [ ] Переконатися, що системна іконка та логотип ресторану завантажуються в заголовок форми.
3. [ ] Перевірити коректність оновлення бейджа активного користувача після авторизації.
