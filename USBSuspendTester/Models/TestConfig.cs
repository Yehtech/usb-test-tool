namespace USBSuspendTester.Models;

/// <summary>
/// Test configuration parameters for a suspend/resume cycle test.
/// </summary>
/// <param name="SuspendDelayMs">Milliseconds to wait after disabling before verifying status</param>
/// <param name="ResumeDelayMs">Milliseconds to wait after enabling before verifying status</param>
/// <param name="CycleIntervalMs">Milliseconds to wait between cycles</param>
/// <param name="MaxCycles">Maximum number of cycles (0 = unlimited)</param>
public sealed record TestConfig(
    int SuspendDelayMs,
    int ResumeDelayMs,
    int CycleIntervalMs,
    int MaxCycles
);

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
