namespace USBSuspendTester.Models;

/// <summary>
/// Message passed from the test runner background task to the UI via ConcurrentQueue.
/// </summary>
/// <param name="Event">The type of event</param>
/// <param name="Text">Display text for the log</param>
/// <param name="Stats">Updated stats snapshot (for CycleComplete/TestStopped events)</param>
/// <param name="Result">Cycle result (for CycleComplete events)</param>
public sealed record TestMessage(
    TestEvent Event,
    string Text,
    TestStats? Stats = null,
    CycleResult? Result = null
);

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
