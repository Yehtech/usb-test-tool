namespace USBSuspendTester.Models;

/// <summary>
/// Types of events that can occur during testing.
/// </summary>
public enum TestEvent
{
    LogInfo,
    LogPass,
    LogFail,
    LogWarn,
    LogSystem,
    CycleComplete,
    TestStarted,
    TestStopped
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
