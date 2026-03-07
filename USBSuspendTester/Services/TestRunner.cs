using System.Collections.Concurrent;
using System.Diagnostics;
using USBSuspendTester.Models;

namespace USBSuspendTester.Services;

/// <summary>
/// Runs suspend/resume test cycles on a background Task.
/// Communicates with the UI via a ConcurrentQueue of TestMessage.
/// </summary>
public sealed class TestRunner
{
    private readonly ConcurrentQueue<TestMessage> _messageQueue;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public ConcurrentQueue<TestMessage> MessageQueue => _messageQueue;
    public bool IsRunning => _runningTask is { IsCompleted: false };

    public TestRunner()
    {
        _messageQueue = new ConcurrentQueue<TestMessage>();
    }

    /// <summary>
    /// Starts the test loop on a background task.
    /// </summary>
    public void Start(USBDevice device, TestConfig config)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        _runningTask = Task.Run(() => RunLoop(device, config, token), token);
    }

    /// <summary>
    /// Signals the test loop to stop. The loop will re-enable the device before exiting.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// Waits for the test loop to finish (with timeout).
    /// </summary>
    public async Task WaitForCompletionAsync(int timeoutMs = 15_000)
    {
        if (_runningTask is not null)
        {
            await Task.WhenAny(_runningTask, Task.Delay(timeoutMs));
        }
    }

    private void Enqueue(TestEvent evt, string text, TestStats? stats = null, CycleResult? result = null)
    {
        _messageQueue.Enqueue(new TestMessage(evt, text, stats, result));
    }

    private async Task RunLoop(USBDevice device, TestConfig config, CancellationToken token)
    {
        var stats = new TestStats(0, 0, 0, DateTime.Now);

        Enqueue(TestEvent.TestStarted,
            $"Test started on: {device.Description} ({device.Vid}:{device.Pid})");
        Enqueue(TestEvent.LogSystem,
            $"Instance ID: {device.InstanceId}");
        Enqueue(TestEvent.LogSystem,
            $"Parameters: Suspend={config.SuspendDelayMs}ms, Resume={config.ResumeDelayMs}ms, " +
            $"Interval={config.CycleIntervalMs}ms, MaxCycles={config.MaxCycles}");

        try
        {
            int cycleNum = 0;
            while (!token.IsCancellationRequested)
            {
                cycleNum++;
                if (config.MaxCycles > 0 && cycleNum > config.MaxCycles)
                {
                    Enqueue(TestEvent.LogSystem,
                        $"Reached maximum cycle count ({config.MaxCycles}). Stopping.");
                    break;
                }

                var cycleTimer = Stopwatch.StartNew();
                bool suspendOk = false;
                bool resumeOk = false;

                // --- SUSPEND ---
                Enqueue(TestEvent.LogInfo, $"--- Cycle {cycleNum} ---");
                Enqueue(TestEvent.LogInfo, "Disabling device (Suspend)...");

                bool disableResult = USBManager.DisableDevice(device.InstanceId);
                if (!disableResult)
                {
                    Enqueue(TestEvent.LogFail, "pnputil disable command failed!");
                }
                else
                {
                    Enqueue(TestEvent.LogInfo,
                        $"Waiting {config.SuspendDelayMs}ms for suspend to take effect...");

                    await Task.Delay(config.SuspendDelayMs, token);

                    string status = USBManager.GetDeviceStatus(device.InstanceId);
                    if (USBManager.IsDeviceDisabled(status))
                    {
                        suspendOk = true;
                        Enqueue(TestEvent.LogPass, $"Suspend OK (Status: {status})");
                    }
                    else
                    {
                        Enqueue(TestEvent.LogWarn,
                            $"Suspend verification uncertain (Status: {status})");
                    }
                }

                if (token.IsCancellationRequested) break;

                // --- RESUME ---
                Enqueue(TestEvent.LogInfo, "Enabling device (Resume)...");
                bool enableResult = USBManager.EnableDevice(device.InstanceId);
                if (!enableResult)
                {
                    Enqueue(TestEvent.LogFail, "pnputil enable command failed!");
                }
                else
                {
                    Enqueue(TestEvent.LogInfo,
                        $"Waiting {config.ResumeDelayMs}ms for resume to take effect...");

                    await Task.Delay(config.ResumeDelayMs, token);

                    string status = USBManager.GetDeviceStatus(device.InstanceId);
                    if (USBManager.IsDeviceStarted(status))
                    {
                        resumeOk = true;
                        Enqueue(TestEvent.LogPass, $"Resume OK (Status: {status})");
                    }
                    else
                    {
                        Enqueue(TestEvent.LogWarn,
                            $"Resume verification uncertain (Status: {status})");
                    }
                }

                // --- Cycle result ---
                cycleTimer.Stop();
                bool cyclePassed = suspendOk && resumeOk;

                stats = stats with
                {
                    Total = stats.Total + 1,
                    Passed = stats.Passed + (cyclePassed ? 1 : 0),
                    Failed = stats.Failed + (cyclePassed ? 0 : 1),
                };

                var cycleResult = new CycleResult(
                    cycleNum, suspendOk, resumeOk, cycleTimer.Elapsed.TotalSeconds);

                if (cyclePassed)
                    Enqueue(TestEvent.LogPass,
                        $"Cycle {cycleNum} PASSED ({cycleResult.ElapsedSeconds:F1}s)");
                else
                    Enqueue(TestEvent.LogFail,
                        $"Cycle {cycleNum} FAILED ({cycleResult.ElapsedSeconds:F1}s)");

                Enqueue(TestEvent.CycleComplete, string.Empty, stats, cycleResult);

                // --- Interval ---
                if (!token.IsCancellationRequested && config.CycleIntervalMs > 0)
                {
                    await Task.Delay(config.CycleIntervalMs, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when Stop() is called
        }
        catch (Exception ex)
        {
            Enqueue(TestEvent.LogFail, $"Unexpected error: {ex.Message}");
        }
        finally
        {
            // Safety: query actual device status and re-enable if needed
            try
            {
                string currentStatus = USBManager.GetDeviceStatus(device.InstanceId);
                if (!USBManager.IsDeviceStarted(currentStatus))
                {
                    Enqueue(TestEvent.LogWarn,
                        $"Device status is '{currentStatus}'. Re-enabling before exit...");
                    bool reEnabled = USBManager.EnableDevice(device.InstanceId);
                    if (reEnabled)
                    {
                        string verifyStatus = USBManager.GetDeviceStatus(device.InstanceId);
                        if (USBManager.IsDeviceStarted(verifyStatus))
                            Enqueue(TestEvent.LogPass, "Device re-enabled successfully.");
                        else
                            Enqueue(TestEvent.LogFail,
                                $"Device re-enable uncertain (Status: {verifyStatus}). Check Device Manager.");
                    }
                    else
                    {
                        Enqueue(TestEvent.LogFail,
                            "Failed to re-enable device! Please check Device Manager.");
                    }
                }
            }
            catch (Exception ex)
            {
                Enqueue(TestEvent.LogFail,
                    $"Error during device recovery: {ex.Message}. Check Device Manager.");
            }

            Enqueue(TestEvent.TestStopped, "Test stopped.", stats);
        }
    }
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
