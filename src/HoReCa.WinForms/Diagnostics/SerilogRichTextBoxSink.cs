using System.Drawing;
using Serilog.Core;
using Serilog.Events;

namespace HoReCa.WinForms.Diagnostics;

public class SerilogRichTextBoxSink : ILogEventSink
{
    private static RichTextBox? _targetBox;
    private static readonly object _lock = new();

    public static void SetTarget(RichTextBox box)
    {
        lock (_lock)
        {
            _targetBox = box;
        }
    }

    public void Emit(LogEvent logEvent)
    {
        if (_targetBox == null || _targetBox.IsDisposed) return;

        string message = $"[{logEvent.Timestamp:HH:mm:ss} {logEvent.Level.ToString().ToUpper()[..3]}] {logEvent.RenderMessage()}";
        if (logEvent.Exception != null)
        {
            message += Environment.NewLine + logEvent.Exception;
        }

        Color color = logEvent.Level switch
        {
            LogEventLevel.Verbose => Color.Gray,
            LogEventLevel.Debug => Color.LightSlateGray,
            LogEventLevel.Information => Color.WhiteSmoke,
            LogEventLevel.Warning => Color.Gold,
            LogEventLevel.Error or LogEventLevel.Fatal => Color.IndianRed,
            _ => Color.White
        };

        try
        {
            if (_targetBox.InvokeRequired)
            {
                _targetBox.BeginInvoke(new Action(() => AppendColoredText(message, color)));
            }
            else
            {
                AppendColoredText(message, color);
            }
        }
        catch
        {
            // Ignore UI dispatch failures during shutdown
        }
    }

    private static void AppendColoredText(string text, Color color)
    {
        if (_targetBox == null || _targetBox.IsDisposed) return;

        _targetBox.SelectionStart = _targetBox.TextLength;
        _targetBox.SelectionLength = 0;
        _targetBox.SelectionColor = color;
        _targetBox.AppendText(text + Environment.NewLine);
        _targetBox.ScrollToCaret();
    }
}
