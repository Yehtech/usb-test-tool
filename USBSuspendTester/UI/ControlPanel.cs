using USBSuspendTester.Models;

namespace USBSuspendTester.UI;

/// <summary>
/// UserControl with test parameter inputs, Start/Stop button, and live stats display.
/// </summary>
public sealed class ControlPanel : UserControl
{
    private readonly TextBox _txtSuspendDelay;
    private readonly TextBox _txtResumeDelay;
    private readonly TextBox _txtInterval;
    private readonly TextBox _txtMaxCycles;
    private readonly Button _btnStartStop;
    private readonly Label _lblCycles;
    private readonly Label _lblPassed;
    private readonly Label _lblFailed;
    private readonly Label _lblElapsed;
    private readonly System.Windows.Forms.Timer _elapsedTimer;
    private DateTime _testStartTime;
    private bool _isRunning;

    /// <summary>Raised when the user clicks START.</summary>
    public event Action? OnStartTest;

    /// <summary>Raised when the user clicks STOP.</summary>
    public event Action? OnStopTest;

    public ControlPanel()
    {
        Dock = DockStyle.Top;
        Height = 100;
        BackColor = Constants.PanelColor;
        Padding = new Padding(8, 4, 8, 4);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 9,
            RowCount = 2,
            BackColor = Constants.PanelColor,
        };

        // Column proportions: params(4 label+input pairs) | button | stats(4 labels)
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // lbl
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65)); // txt
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // button + stats

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        // Row 0: Parameter inputs
        _txtSuspendDelay = AddParamField(layout, "Suspend (s):", Constants.DefaultSuspendDelay.ToString("F1"), 0, 0);
        _txtResumeDelay = AddParamField(layout, "Resume (s):", Constants.DefaultResumeDelay.ToString("F1"), 0, 2);
        _txtInterval = AddParamField(layout, "Interval (s):", Constants.DefaultCycleInterval.ToString("F1"), 0, 4);
        _txtMaxCycles = AddParamField(layout, "Max Cycles:", Constants.DefaultMaxCycles.ToString(), 0, 6);

        // Start/Stop button (spans right column, both rows)
        _btnStartStop = new Button
        {
            Text = "START TEST",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Constants.BtnStartBg,
            ForeColor = Constants.BtnTextColor,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 4, 0, 4),
        };
        _btnStartStop.FlatAppearance.BorderSize = 0;
        _btnStartStop.Click += OnStartStopClick;
        layout.Controls.Add(_btnStartStop, 8, 0);
        layout.SetRowSpan(_btnStartStop, 2);

        // Row 1: Stats labels
        _lblCycles = CreateStatLabel("Cycles: 0");
        _lblPassed = CreateStatLabel("Passed: 0", Constants.LogPassColor);
        _lblFailed = CreateStatLabel("Failed: 0", Constants.LogFailColor);
        _lblElapsed = CreateStatLabel("Elapsed: 00:00:00");

        var statsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Constants.PanelColor,
            Margin = new Padding(0),
        };
        statsPanel.Controls.Add(_lblCycles);
        statsPanel.Controls.Add(_lblPassed);
        statsPanel.Controls.Add(_lblFailed);
        statsPanel.Controls.Add(_lblElapsed);

        layout.Controls.Add(statsPanel, 0, 1);
        layout.SetColumnSpan(statsPanel, 8);

        Controls.Add(layout);

        // Elapsed time timer
        _elapsedTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _elapsedTimer.Tick += (_, _) =>
        {
            TimeSpan elapsed = DateTime.Now - _testStartTime;
            _lblElapsed.Text = $"Elapsed: {elapsed:hh\\:mm\\:ss}";
        };
    }

    private TextBox AddParamField(TableLayoutPanel layout, string label, string defaultValue, int row, int col)
    {
        var lbl = new Label
        {
            Text = label,
            ForeColor = Constants.TextColor,
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(4, 0, 2, 0),
        };

        var txt = new TextBox
        {
            Text = defaultValue,
            BackColor = Constants.BgColor,
            ForeColor = Constants.TextColor,
            Font = new Font("Consolas", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 8, 4),
        };

        layout.Controls.Add(lbl, col, row);
        layout.Controls.Add(txt, col + 1, row);
        return txt;
    }

    private static Label CreateStatLabel(string text, Color? color = null)
    {
        return new Label
        {
            Text = text,
            ForeColor = color ?? Constants.TextColor,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(4, 6, 12, 0),
        };
    }

    private void OnStartStopClick(object? sender, EventArgs e)
    {
        if (_isRunning)
        {
            OnStopTest?.Invoke();
        }
        else
        {
            OnStartTest?.Invoke();
        }
    }

    /// <summary>
    /// Parses and validates the parameter fields. Returns null if validation fails.
    /// </summary>
    public TestConfig? GetConfig()
    {
        if (!double.TryParse(_txtSuspendDelay.Text, out double suspendDelay) || suspendDelay < 0)
        {
            ShowValidationError("Suspend Delay must be a non-negative number.");
            return null;
        }
        if (!double.TryParse(_txtResumeDelay.Text, out double resumeDelay) || resumeDelay < 0)
        {
            ShowValidationError("Resume Delay must be a non-negative number.");
            return null;
        }
        if (!double.TryParse(_txtInterval.Text, out double interval) || interval < 0)
        {
            ShowValidationError("Interval must be a non-negative number.");
            return null;
        }
        if (!int.TryParse(_txtMaxCycles.Text, out int maxCycles) || maxCycles < 0)
        {
            ShowValidationError("Max Cycles must be a non-negative integer.");
            return null;
        }

        return new TestConfig(
            (int)(suspendDelay * 1000),
            (int)(resumeDelay * 1000),
            (int)(interval * 1000),
            maxCycles
        );
    }

    private static void ShowValidationError(string message)
    {
        MessageBox.Show(message, "Invalid Parameter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Switches UI to "running" state.
    /// </summary>
    public void SetRunningState()
    {
        _isRunning = true;
        _btnStartStop.Text = "STOP TEST";
        _btnStartStop.BackColor = Constants.BtnStopBg;
        _txtSuspendDelay.Enabled = false;
        _txtResumeDelay.Enabled = false;
        _txtInterval.Enabled = false;
        _txtMaxCycles.Enabled = false;
        _testStartTime = DateTime.Now;
        _elapsedTimer.Start();
    }

    /// <summary>
    /// Switches UI back to "idle" state.
    /// </summary>
    public void SetStoppedState()
    {
        _isRunning = false;
        _btnStartStop.Text = "START TEST";
        _btnStartStop.BackColor = Constants.BtnStartBg;
        _txtSuspendDelay.Enabled = true;
        _txtResumeDelay.Enabled = true;
        _txtInterval.Enabled = true;
        _txtMaxCycles.Enabled = true;
        _elapsedTimer.Stop();
    }

    /// <summary>
    /// Updates the stat labels with the latest test statistics.
    /// </summary>
    public void UpdateStats(TestStats stats)
    {
        _lblCycles.Text = $"Cycles: {stats.Total}";
        _lblPassed.Text = $"Passed: {stats.Passed}";
        _lblFailed.Text = $"Failed: {stats.Failed}";
    }

    /// <summary>
    /// Resets stats display to zero.
    /// </summary>
    public void ResetStats()
    {
        _lblCycles.Text = "Cycles: 0";
        _lblPassed.Text = "Passed: 0";
        _lblFailed.Text = "Failed: 0";
        _lblElapsed.Text = "Elapsed: 00:00:00";
    }
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
