using System.Drawing;
using System.Drawing.Printing;
using HoReCa.Application.DTOs;
using HoReCa.Application.Interfaces;
using HoReCa.Infrastructure.Services;

namespace HoReCa.WinForms.Printing;

public class ReceiptPrinterEngine : IReceiptPrintService
{
    public string GenerateTextReceipt(OrderDto order, string businessName, string cashierName)
    {
        return ReceiptTextFormatter.GenerateTextReceipt(order, businessName, cashierName);
    }

    public void PrintReceipt(OrderDto order, string businessName, string cashierName, bool previewOnly = true)
    {
        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"Receipt_{order.OrderNumber}";

        printDoc.DefaultPageSettings.PaperSize = new PaperSize("Roll80mm", 315, 800);
        printDoc.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

        string text = GenerateTextReceipt(order, businessName, cashierName);

        printDoc.PrintPage += (sender, e) =>
        {
            if (e.Graphics == null) return;

            using var fontRegular = new Font("Courier New", 9f, FontStyle.Regular);
            using var fontBold = new Font("Courier New", 10f, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);

            float y = 10;
            float leftMargin = 10;

            foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var fontToUse = (rawLine.Contains("ФІСКАЛЬНИЙ ЧЕК") || rawLine.Contains("ДО СПЛАТИ:") || rawLine.Contains(businessName.ToUpperInvariant()))
                    ? fontBold
                    : fontRegular;

                e.Graphics.DrawString(rawLine, fontToUse, brush, leftMargin, y);
                y += fontToUse.GetHeight(e.Graphics) + 1;
            }

            e.HasMorePages = false;
        };

        if (previewOnly)
        {
            using var preview = new PrintPreviewDialog
            {
                Document = printDoc,
                Width = 450,
                Height = 650,
                Text = $"Попередній перегляд чека: {order.OrderNumber}"
            };
            preview.ShowDialog();
        }
        else
        {
            printDoc.Print();
        }
    }

    private static string CenterText(string text, int width)
    {
        if (text.Length >= width) return text;
        int leftPad = (width - text.Length) / 2;
        return text.PadLeft(leftPad + text.Length).PadRight(width);
    }
}
