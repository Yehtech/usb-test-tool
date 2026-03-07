using System.Runtime.InteropServices;
using USBSuspendTester.Models;
using USBSuspendTester.Services;

namespace USBSuspendTester.UI;

/// <summary>
/// Main application form: assembles DevicePanel, ControlPanel, and LogPanel.
/// Polls the TestRunner message queue on a WinForms Timer.
/// </summary>
public sealed class MainForm : Form
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private readonly DevicePanel _devicePanel;
    private readonly ControlPanel _controlPanel;
    private readonly LogPanel _logPanel;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private TestRunner? _testRunner;

    public MainForm()
    {
        Text = "USB Suspend/Resume Tester v1.1.0";
        Width = Constants.FormWidth;
        Height = Constants.FormHeight;
        MinimumSize = new Size(Constants.FormMinWidth, Constants.FormMinHeight);
        BackColor = Constants.BgColor;
        StartPosition = FormStartPosition.CenterScreen;

        // Dark title bar
        EnableDarkTitleBar();

        // Panels
        _logPanel = new LogPanel();
        _controlPanel = new ControlPanel();
        _devicePanel = new DevicePanel();

        // Layout
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4),
        };
        container.Controls.Add(_logPanel);
        container.Controls.Add(_controlPanel);
        container.Controls.Add(_devicePanel);
        Controls.Add(container);

        // Wire events
        _controlPanel.OnStartTest += HandleStartTest;
        _controlPanel.OnStopTest += HandleStopTest;

        // Poll timer for message queue
        _pollTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _pollTimer.Tick += PollMessages;
        _pollTimer.Start();

        // Initial device scan
        Load += (_, _) => _devicePanel.RefreshDevices();
    }

    private void EnableDarkTitleBar()
    {
        try
        {
            int value = 1;
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref value, sizeof(int));
        }
        catch (ExternalException)
        {
            // Not supported on older Windows versions — ignore
        }
    }

    private void HandleStartTest()
    {
        USBDevice? device = _devicePanel.GetSelectedDevice();
        if (device is null)
        {
            MessageBox.Show("Please select a USB device first.",
                "No Device Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        TestConfig? config = _controlPanel.GetConfig();
        if (config is null) return;

        _testRunner = new TestRunner();
        _testRunner.Start(device, config);

        _controlPanel.SetRunningState();
        _controlPanel.ResetStats();
        _devicePanel.SetInteractionEnabled(false);
    }

    private void HandleStopTest()
    {
        _testRunner?.Stop();
    }

    private void PollMessages(object? sender, EventArgs e)
    {
        if (_testRunner is null) return;

        while (_testRunner.MessageQueue.TryDequeue(out TestMessage? msg))
        {
            // Log all text messages
            if (!string.IsNullOrEmpty(msg.Text))
            {
                _logPanel.AppendLog(msg.Event, msg.Text);
            }

            // Update stats on cycle complete
            if (msg.Stats is not null)
            {
                _controlPanel.UpdateStats(msg.Stats);
            }

            // Handle test stopped
            if (msg.Event == TestEvent.TestStopped)
            {
                _controlPanel.SetStoppedState();
                _devicePanel.SetInteractionEnabled(true);
                _devicePanel.RefreshDevices();
            }
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (_testRunner is { IsRunning: true })
        {
            DialogResult result = MessageBox.Show(
                "A test is currently running. Stop the test and close?",
                "Confirm Close",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _testRunner.Stop();
            await _testRunner.WaitForCompletionAsync();
        }

        _pollTimer.Stop();
        base.OnFormClosing(e);
    }
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
