# Lesson 19: Configuration Patterns & Options Binding via appsettings.json

## 1. Overview

Enterprise software must never hardcode connection strings, business names, tax rates, or printer names. In this lesson, we bind strongly-typed options from `appsettings.json` into the DI container using `IOptions<T>`.

---

## 2. Configuration Structure (`appsettings.json`)

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=horeca_mastery.db"
  },
  "HoReCaSettings": {
    "BusinessName": "GrandLux Hotel & Restaurant",
    "TaxRatePercent": 20.0,
    "DefaultServiceChargePercent": 5.0,
    "ReceiptPrinterName": "POS-80C",
    "KdsRefreshIntervalSeconds": 5
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

---

## 3. Strongly-Typed Options Class

```csharp
public class HoReCaOptions
{
    public const string SectionName = "HoReCaSettings";

    public string BusinessName { get; set; } = "GrandLux Hotel & Restaurant";
    public decimal TaxRatePercent { get; set; } = 20.0m;
    public decimal DefaultServiceChargePercent { get; set; } = 0m;
    public string ReceiptPrinterName { get; set; } = "Default";
    public int KdsRefreshIntervalSeconds { get; set; } = 5;
}
```

---

## 4. Registering & Consuming Options

### Registration in `Program.cs`

```csharp
services.Configure<HoReCaOptions>(
    configuration.GetSection(HoReCaOptions.SectionName));
```

### Consumption in Services & Forms

```csharp
public class ReceiptPrinterEngine : IReceiptPrintService
{
    private readonly HoReCaOptions _options;

    public ReceiptPrinterEngine(IOptions<HoReCaOptions> options)
    {
        _options = options.Value;
    }

    public void PrintReceipt(OrderDto order, string cashierName)
    {
        // Use configured business name and printer
        string text = ReceiptTextFormatter.GenerateTextReceipt(order, _options.BusinessName, cashierName);
        // ...
    }
}
```

---

## 5. Key Takeaways

1. **`IOptionsSnapshot<T>`**: In long-running desktop apps, using `IOptionsSnapshot<T>` allows reading updated configuration changes immediately when `appsettings.json` is edited on disk without restarting the application.
2. **Environment Variable Overrides**: Cloud deployments or staging CI can override settings via `HoReCaSettings__BusinessName="Staging Complex"`.
