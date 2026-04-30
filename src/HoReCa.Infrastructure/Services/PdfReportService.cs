using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HoReCa.Infrastructure.Services;

public class PdfReportService : IPdfReportService
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<string> GenerateReceiptPdfAsync(OrderDto order, string cashierName, CancellationToken ct = default)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "receipts");
        Directory.CreateDirectory(dir);
        string filename = $"Receipt_{order.OrderNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        string filePath = Path.Combine(dir, filename);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(15);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Spacing(2);
                    col.Item().AlignCenter().Text("LUXERESTO HORECA OS").Bold().FontSize(13).FontColor(Colors.Grey.Darken4);
                    col.Item().AlignCenter().Text("ГрандЛюкс Резорт & Ресторан").FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                    col.Item().AlignCenter().Text("м. Київ, вул. Паркова, 12 • Тел: +38 (044) 777-88-99").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignCenter().Text($"ІПН 398472910482 • ФІСКАЛЬНИЙ ЧЕК").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Чек: {order.OrderNumber}").Bold();
                        r.RelativeItem().AlignRight().Text($"Стіл: #{order.TableNumber ?? 0}").Bold();
                    });

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Офіціант: {order.WaiterName}").FontSize(8);
                        r.RelativeItem().AlignRight().Text($"Касир: {cashierName}").FontSize(8);
                    });

                    col.Item().Text($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}").FontSize(8);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Найменування").Bold().FontSize(8);
                            header.Cell().AlignCenter().Text("К-сть").Bold().FontSize(8);
                            header.Cell().AlignRight().Text("Ціна").Bold().FontSize(8);
                            header.Cell().AlignRight().Text("Сума").Bold().FontSize(8);
                        });

                        foreach (var item in order.Items)
                        {
                            table.Cell().Text(item.ItemName).FontSize(8);
                            table.Cell().AlignCenter().Text(item.Quantity.ToString()).FontSize(8);
                            table.Cell().AlignRight().Text($"{item.UnitPrice:F2}").FontSize(8);
                            table.Cell().AlignRight().Text($"{item.TotalPrice:F2}").FontSize(8);

                            if (!string.IsNullOrWhiteSpace(item.ModifiersOrNotes))
                            {
                                table.Cell().ColumnSpan(4).PaddingLeft(6).Text($"* {item.ModifiersOrNotes}").FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                            }
                        }
                    });

                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("Підсумок:").FontSize(8.5f);
                        r.RelativeItem().AlignRight().Text($"{order.Subtotal:F2} ₴").FontSize(8.5f);
                    });

                    if (order.DiscountPercent > 0)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Знижка ({order.DiscountPercent:F0}%):").FontSize(8.5f);
                            r.RelativeItem().AlignRight().Text($"-{(order.Subtotal * order.DiscountPercent / 100m):F2} ₴").FontSize(8.5f);
                        });
                    }

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("ПДВ (20% включено):").FontSize(8).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text($"{(order.TotalAmount * 0.20m / 1.20m):F2} ₴").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("ДО СПЛАТИ:").Bold().FontSize(12);
                        r.RelativeItem().AlignRight().Text($"{order.TotalAmount:F2} ₴").Bold().FontSize(12).FontColor(Colors.Green.Darken2);
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Spacing(2);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    col.Item().AlignCenter().Text("ДЯКУЄМО ЗА ВІЗИТ! ☕").Bold().FontSize(9);
                    col.Item().AlignCenter().Text("Чайові вітаються, але завжди на ваш розсуд").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignCenter().Text("www.grandlux-resort.ua • Wi-Fi: GrandLux_Guest").FontSize(7f).FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf(filePath);

        return Task.FromResult(filePath);
    }

    public Task<string> GeneratePurchaseOrderPdfAsync(PurchaseOrderDto po, CancellationToken ct = default)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "procurement");
        Directory.CreateDirectory(dir);
        string filename = $"PurchaseOrder_{po.OrderNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        string filePath = Path.Combine(dir, filename);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Spacing(3);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("LUXERESTO HORECA SUITE").Bold().FontSize(16).FontColor(Colors.Blue.Darken3);
                            c.Item().Text("ТОВ «ГрандЛюкс Резорт & Спа» • Код ЄДРПОУ 39847291").FontSize(9);
                            c.Item().Text("м. Київ, вул. Паркова, 12 • supply@grandlux.ua").FontSize(9);
                        });

                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("ВИМОГА-НАКЛАДНА").Bold().FontSize(14).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"№ {po.OrderNumber}").Bold().FontSize(12);
                            c.Item().Text($"Дата: {po.OrderDate:dd.MM.yyyy}").FontSize(9.5f);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

                    col.Item().PaddingTop(8).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("ПОСТАЧАЛЬНИК:").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(po.SupplierName).Bold().FontSize(11);
                        });

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("ТЕРМІН ПОСТАВКИ:").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(po.ExpectedDeliveryDate?.ToString("dd.MM.yyyy") ?? "Терміново (1 робочий день)").FontSize(10);
                        });

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("СТАТУС:").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(po.Status.ToString()).Bold().FontSize(10).FontColor(Colors.Green.Darken2);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("№").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("SKU / Код").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Найменування інгредієнта").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Од.").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Кількість").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Ціна за од.").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Сума грн").Bold().FontSize(9);
                        });

                        int idx = 1;
                        foreach (var item in po.Items)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(idx++.ToString()).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.SKU).FontSize(8.5f);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.ItemName).Bold().FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(item.Unit).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{item.QuantityOrdered:F2}").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{item.UnitCost:F2}").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{item.LineTotal:F2}").Bold().FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(150).Text("ВСЬОГО ДО ОПЛАТИ:").Bold().FontSize(12);
                            r.ConstantItem(120).AlignRight().Text($"{po.TotalAmount:F2} ₴").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                        });
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(150).Text("в т.ч. ПДВ (20%):").FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                            r.ConstantItem(120).AlignRight().Text($"{(po.TotalAmount * 0.20m / 1.20m):F2} ₴").FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(po.Notes))
                    {
                        col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("Примітки та особливі умови доставки:").Bold().FontSize(8.5f);
                            c.Item().Text(po.Notes).FontSize(8.5f);
                        });
                    }

                    col.Item().PaddingTop(25).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Замовлення склав (Зав. складом):").FontSize(9);
                            c.Item().PaddingTop(18).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                            c.Item().Text("Бондар В. М. / підпис").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        r.ConstantItem(40);

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Затвердив (Шеф-кухар / Керуючий):").FontSize(9);
                            c.Item().PaddingTop(18).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                            c.Item().Text("Коваленко О. І. / підпис").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        r.ConstantItem(40);

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Постачальник (Прийняв в роботу):").FontSize(9);
                            c.Item().PaddingTop(18).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                            c.Item().Text("Дата та підпис представника").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Документ сформовано автоматично модулем ERP Склад системи LuxeResto HoReCa Suite • Сторінка ");
                    t.CurrentPageNumber();
                    t.Span(" з ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf(filePath);

        return Task.FromResult(filePath);
    }

    public Task<string> GenerateZReportPdfAsync(AnalyticsSummaryDto summary, string cashierName, CancellationToken ct = default)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "reports");
        Directory.CreateDirectory(dir);
        string filename = $"ZReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        string filePath = Path.Combine(dir, filename);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("LUXERESTO HORECA SUITE — BI & ФІНАНСИ").Bold().FontSize(14).FontColor(Colors.Grey.Darken4);
                            c.Item().Text("ДЕННИЙ ФІНАНСОВИЙ Z-ЗВІТ ЗМІНИ").Bold().FontSize(12).FontColor(Colors.Green.Darken3);
                            c.Item().Text($"Дата звіту: {DateTime.Now:dd.MM.yyyy HH:mm:ss}").FontSize(9);
                        });

                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Касир / Адміністратор:").FontSize(9);
                            c.Item().Text(cashierName).Bold().FontSize(10);
                            c.Item().Text("Зміна: Денна (№ 142)").FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Colors.Green.Darken3);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("ВИРУЧКА ЗА ДЕНЬ:").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{summary.TodayRevenue:N2} ₴").Bold().FontSize(16).FontColor(Colors.Green.Darken3);
                        });

                        r.ConstantItem(10);

                        r.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("ЗАМОВЛЕНЬ У РЕСТОРАНІ:").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{summary.TodayOrdersCount}").Bold().FontSize(16);
                        });

                        r.ConstantItem(10);

                        r.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("СЕРЕДНІЙ ЧЕК:").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{summary.AverageCheck:N2} ₴").Bold().FontSize(16).FontColor(Colors.Amber.Darken3);
                        });

                        r.ConstantItem(10);

                        r.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("ЗАВАНТАЖЕНІСТЬ ГОТЕЛЮ:").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{summary.HotelOccupancyPercent:F1}%").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                        });
                    });

                    col.Item().PaddingTop(8).Text("ТОП-5 НАЙБІЛЬШ ПРОДАВАНИХ СТРАВ ЗА ЗМІНУ:").Bold().FontSize(11);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("№").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Назва страви / напою").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Порцій").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Виручка грн").Bold().FontSize(9);
                        });

                        int i = 1;
                        foreach (var dish in summary.TopDishes)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(i++.ToString()).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(dish.Name).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(dish.QuantitySold.ToString()).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{dish.TotalRevenue:N2} ₴").Bold().FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(25).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Звіт сформував касир:").FontSize(9);
                            c.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                            c.Item().Text($"{cashierName} / підпис").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        r.ConstantItem(60);

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Головний бухгалтер / Керуючий:").FontSize(9);
                            c.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                            c.Item().Text("Коваленко О. І. / підпис").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("LuxeResto HoReCa Suite • Фіскальний підсумок дня • Сторінка ");
                    t.CurrentPageNumber();
                    t.Span(" з ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf(filePath);

        return Task.FromResult(filePath);
    }
}
