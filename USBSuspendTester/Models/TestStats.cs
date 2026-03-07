namespace USBSuspendTester.Models;

/// <summary>
/// Cumulative test statistics updated after each cycle.
/// </summary>
/// <param name="Total">Total cycles completed</param>
/// <param name="Passed">Number of successful cycles</param>
/// <param name="Failed">Number of failed cycles</param>
/// <param name="StartTime">When the test session started</param>
public sealed record TestStats(
    int Total,
    int Passed,
    int Failed,
    DateTime StartTime
);

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
