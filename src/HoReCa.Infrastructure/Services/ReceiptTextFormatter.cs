using System.Globalization;
using System.Text;
using HoReCa.Application.DTOs;

namespace HoReCa.Infrastructure.Services;

public static class ReceiptTextFormatter
{
    public static string GenerateTextReceipt(OrderDto order, string businessName, string cashierName)
    {
        var sb = new StringBuilder();
        string line = new('=', 40);
        string dash = new('-', 40);

        sb.AppendLine(CenterText(businessName.ToUpperInvariant(), 40));
        sb.AppendLine(CenterText("ГОТЕЛЬНО-РЕСТОРАННИЙ КОМПЛЕКС", 40));
        sb.AppendLine(CenterText("м. Чернівці, вул. Головна, 100", 40));
        sb.AppendLine(CenterText("ПН 382910481940 | ФІСКАЛЬНИЙ ЧЕК", 40));
        sb.AppendLine(line);
        sb.AppendLine($"ЧЕК №: {order.OrderNumber}");
        sb.AppendLine($"ДАТА: {order.CreatedAt:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine($"КАСИР / ОФІЦІАНТ: {cashierName}");
        if (order.TableNumber.HasValue)
            sb.AppendLine($"СТОЛИК: № {order.TableNumber.Value}");
        sb.AppendLine(dash);

        sb.AppendLine(string.Format("{0,-20} {1,4} {2,6} {3,7}", "НАЗВА", "К-ТЬ", "ЦІНА", "СУМА"));
        sb.AppendLine(dash);

        foreach (var item in order.Items)
        {
            string name = item.ItemName.Length > 20 ? item.ItemName[..19] + "…" : item.ItemName;
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-20} {1,4} {2,6:F0} {3,7:F2}", name, item.Quantity, item.UnitPrice, item.TotalPrice));
            if (!string.IsNullOrWhiteSpace(item.ModifiersOrNotes))
            {
                sb.AppendLine($"  * {item.ModifiersOrNotes}");
            }
        }

        sb.AppendLine(dash);
        sb.AppendLine($"ПІДСУМОК:                      {order.Subtotal.ToString("F2", CultureInfo.InvariantCulture),10} ₴");
        if (order.DiscountPercent > 0)
        {
            decimal discount = order.Subtotal * (order.DiscountPercent / 100m);
            sb.AppendLine($"ЗНИЖКА ({order.DiscountPercent}%):               -{discount.ToString("F2", CultureInfo.InvariantCulture),9} ₴");
        }
        sb.AppendLine(line);
        sb.AppendLine($"ДО СПЛАТИ:                     {order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),10} ₴");
        sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine(CenterText("ДЯКУЄМО ЗА ВІЗИТ!", 40));
        sb.AppendLine(CenterText("WI-FI: GrandLux_Guest / PASS: luxury2026", 40));
        sb.AppendLine(CenterText("www.grandlux-hotel.ua", 40));
        sb.AppendLine();
        sb.AppendLine(CenterText("[ QR-CODE PAYMENT VALIDATION ]", 40));

        return sb.ToString();
    }

    private static string CenterText(string text, int width)
    {
        if (text.Length >= width) return text;
        int leftPadding = (width - text.Length) / 2;
        return text.PadLeft(leftPadding + text.Length).PadRight(width);
    }
}
