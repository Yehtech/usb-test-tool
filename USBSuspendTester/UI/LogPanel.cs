using USBSuspendTester.Models;

namespace USBSuspendTester.UI;

/// <summary>
/// UserControl displaying a colored real-time log with Clear and Export buttons.
/// </summary>
public sealed class LogPanel : UserControl
{
    private readonly RichTextBox _logBox;
    private readonly Button _btnClear;
    private readonly Button _btnExport;

    public LogPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Constants.PanelColor;
        Padding = new Padding(8);

        // Header with label and buttons
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Constants.PanelColor,
        };

        var lblTitle = new Label
        {
            Text = "Test Log",
            ForeColor = Constants.TextColor,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 8),
        };

        _btnExport = CreateButton("Export", 70);
        _btnExport.Click += OnExportClick;

        _btnClear = CreateButton("Clear", 60);
        _btnClear.Click += (_, _) => _logBox!.Clear();

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            BackColor = Constants.PanelColor,
        };
        btnPanel.Controls.Add(_btnExport);
        btnPanel.Controls.Add(_btnClear);

        header.Controls.Add(lblTitle);
        header.Controls.Add(btnPanel);

        // Log text box
        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Constants.BgColor,
            ForeColor = Constants.TextColor,
            Font = new Font("Consolas", 10f),
            BorderStyle = BorderStyle.None,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
        };

        Controls.Add(_logBox);
        Controls.Add(header);
    }

    private static Button CreateButton(string text, int width)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Constants.BtnDefaultBg,
            ForeColor = Constants.BtnTextColor,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 2, 0, 2),
        };
    }

    /// <summary>
    /// Appends a log line with timestamp and color based on the event type.
    /// </summary>
    public void AppendLog(TestEvent evt, string text)
    {
        if (_logBox.IsDisposed) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string prefix = evt switch
        {
            TestEvent.LogInfo => "[INFO]",
            TestEvent.LogPass => "[PASS]",
            TestEvent.LogFail => "[FAIL]",
            TestEvent.LogWarn => "[WARN]",
            TestEvent.LogSystem => "[SYS ]",
            TestEvent.TestStarted => "[SYS ]",
            TestEvent.TestStopped => "[SYS ]",
            _ => "[    ]",
        };

        Color color = evt switch
        {
            TestEvent.LogInfo => Constants.LogInfoColor,
            TestEvent.LogPass => Constants.LogPassColor,
            TestEvent.LogFail => Constants.LogFailColor,
            TestEvent.LogWarn => Constants.LogWarnColor,
            TestEvent.LogSystem => Constants.LogSysColor,
            TestEvent.TestStarted => Constants.LogSysColor,
            TestEvent.TestStopped => Constants.LogSysColor,
            _ => Constants.TextColor,
        };

        string line = $"{timestamp} {prefix} {text}\n";

        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.SelectionLength = 0;
        _logBox.SelectionColor = color;
        _logBox.AppendText(line);
        _logBox.ScrollToCaret();
    }

    private void OnExportClick(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = $"usb_test_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _logBox.Text, System.Text.Encoding.UTF8);
        }
    }
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
