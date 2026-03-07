namespace USBSuspendTester.Models;

/// <summary>
/// Result of a single suspend/resume test cycle.
/// </summary>
/// <param name="CycleNumber">1-based cycle number</param>
/// <param name="SuspendOk">Whether the device was successfully suspended (disabled)</param>
/// <param name="ResumeOk">Whether the device was successfully resumed (enabled)</param>
/// <param name="ElapsedSeconds">Total seconds taken for this cycle</param>
public sealed record CycleResult(
    int CycleNumber,
    bool SuspendOk,
    bool ResumeOk,
    double ElapsedSeconds
);

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
