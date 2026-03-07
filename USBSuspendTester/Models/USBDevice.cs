namespace USBSuspendTester.Models;

/// <summary>
/// Represents a connected USB device with its identification and status information.
/// </summary>
/// <param name="InstanceId">PnP device instance path (e.g., USB\VID_1234&PID_5678\...)</param>
/// <param name="Vid">Vendor ID extracted from Instance ID</param>
/// <param name="Pid">Product ID extracted from Instance ID</param>
/// <param name="Description">Device description from pnputil</param>
/// <param name="Manufacturer">Manufacturer name from pnputil</param>
/// <param name="Status">Current device status (Started, Disabled, etc.)</param>
public sealed record USBDevice(
    string InstanceId,
    string Vid,
    string Pid,
    string Description,
    string Manufacturer,
    string Status
);

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
