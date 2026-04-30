# Практичний гайд 20: Підготовка до продакшену, Single-File публікація та розгортання

## 1. Теоретична база та сценарії дистрибуції

На фінальному етапі розробки десктопного рішення для сектору HoReCa постає завдання розгортання:
- Застосунок повинен встановлюватися на сенсорні моноблоки касирів, робочі станції портьє та кухонні планшети з мінімальними вимогами до ІТ-персоналу.
- На клієнтських комп'ютерах **може бути не встановлений .NET SDK або .NET Runtime**.
- Застосунок повинен бути зібраний як єдиний виконуваний файл (*Single-File Executable*) з усіма вбудованими ресурсами (іконками, шрифтами, емблемою).

---

## 2. Архітектурна схема конвеєра публікації

```mermaid
flowchart TD
    DevCode["Вихідний код рішень (.NET 10 LTS WinForms)"] --> Tests["Запуск регресійних тестів: dotnet test (30/30 Passed)"]
    Tests --> PublishCmd["Команда dotnet publish з прапорцями оптимізації"]
    
    PublishCmd --> Flag1["-c Release (Оптимізація коду компілятором Roslyn)"]
    PublishCmd --> Flag2["-r win-x64 (Цільова платформа Windows 64-bit)"]
    PublishCmd --> Flag3["--self-contained true (Включення .NET 10 Runtime у бандл)"]
    PublishCmd --> Flag4["-p:PublishSingleFile=true (Упаковка в єдиний EXE-файл)"]
    
    Flag1 & Flag2 & Flag3 & Flag4 --> OutputExe["Готовий виконуваний файл: LuxeResto.exe (~80-100 МБ)"]
    OutputExe --> CopyAssets["Копіювання конфігурації appsettings.json та папки Assets"]
    CopyAssets --> ReadyDeploy["Готово до розгортання на терміналах ресторану та готелю"]
```

---

## 3. Детальний розбір команди публікації

### 3.1. Команда збірки самодостатнього пакету:

```powershell
dotnet publish "src/HoReCa.WinForms/HoReCa.WinForms.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "./publish/win-x64"
```

### 3.2. Налаштування файлу проєкту: [`HoReCa.WinForms.csproj`](file:///C:/Users/Yurii/source/repos/- MASTERY/WinForms-HoReCa-Mastery/src/HoReCa.WinForms/HoReCa.WinForms.csproj)

```xml
<PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>Assets\app.ico</ApplicationIcon>
</PropertyGroup>

<ItemGroup>
    <None Update="appsettings.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="Assets\**">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

---

## 4. Підводні камені та типові помилки

> [!WARNING]
> **Шляхи до файлів у Single-File застосунках:**
> У Single-File виконуваному файлі `Assembly.GetExecutingAssembly().Location` повертає порожній рядок або тимчасову системну папку розпаковки (`Temp`). Завжди використовуйте `AppContext.BaseDirectory` для визначення фізичного розташування файлів поруч із `.exe` (наприклад, для папки `Assets` та бази `horeca_local.db`).

---

## 5. Практичний чек-лист для закріплення
1. [ ] Перевірити працездатність скомпільованого `.exe` на «чистій» машині без встановленого .NET SDK.
2. [ ] Переконатися, що папка `Assets/Images` скопійована поруч із виконуваним файлом.
3. [ ] Перевірити коректність створення файлу бази даних при першому запуску.
