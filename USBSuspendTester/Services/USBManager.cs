using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using USBSuspendTester.Models;

namespace USBSuspendTester.Services;

/// <summary>
/// Manages USB device enumeration and control via pnputil.
/// </summary>
public static class USBManager
{
    private static readonly Regex VidPidRegex = new(
        @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})",
        RegexOptions.Compiled);

    /// <summary>
    /// Captures stdout, stderr, and exit code from a pnputil invocation.
    /// </summary>
    private sealed record PnpUtilResult(string Stdout, string Stderr, int ExitCode);

    /// <summary>
    /// Runs pnputil with the given arguments and returns a structured result.
    /// Uses the system OEM code page for proper encoding of localized output.
    /// </summary>
    private static PnpUtilResult RunPnpUtil(params string[] args)
    {
        int oemCodePage = CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
        Encoding encoding = Encoding.GetEncoding(oemCodePage);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = Constants.PnpUtilPath,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding,
        };

        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(Constants.PnpUtilTimeoutMs);
        int exitCode = process.ExitCode;
        return new PnpUtilResult(stdout, stderr, exitCode);
    }

    /// <summary>
    /// Checks whether a pnputil command succeeded by examining exit code and
    /// scanning stdout+stderr for failure keywords in English and Chinese.
    /// </summary>
    private static bool IsCommandSuccessful(PnpUtilResult result)
    {
        if (result.ExitCode != 0)
            return false;

        string combined = result.Stdout + result.Stderr;
        if (combined.Contains("Fail", StringComparison.OrdinalIgnoreCase))
            return false;
        if (combined.Contains("Error", StringComparison.OrdinalIgnoreCase))
            return false;
        if (combined.Contains("\u5931\u6557", StringComparison.Ordinal)) // 失敗
            return false;
        if (combined.Contains("\u932F\u8AA4", StringComparison.Ordinal)) // 錯誤
            return false;

        return true;
    }

    /// <summary>
    /// Finds the matching field value from pnputil output, supporting multiple languages.
    /// </summary>
    private static string GetFieldValue(Dictionary<string, string> fields, string key)
    {
        if (!Constants.FieldKeys.TryGetValue(key, out string[]? aliases))
            return string.Empty;

        foreach (string alias in aliases)
        {
            if (fields.TryGetValue(alias, out string? value))
                return value;
        }
        return string.Empty;
    }

    /// <summary>
    /// Enumerates connected USB devices that have a VID/PID in their instance ID.
    /// </summary>
    public static List<USBDevice> EnumerateDevices()
    {
        PnpUtilResult result = RunPnpUtil(Constants.EnumDevicesArgs);
        var devices = new List<USBDevice>();

        // Parse block-based output: each device block is separated by blank lines
        var currentFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in result.Stdout.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(line))
            {
                // End of a block — try to build a device
                if (currentFields.Count > 0)
                {
                    TryAddDevice(currentFields, devices);
                    currentFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            int colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                string fieldName = line[..colonIdx].Trim();
                string fieldValue = line[(colonIdx + 1)..].Trim();
                currentFields[fieldName] = fieldValue;
            }
        }

        // Handle last block
        if (currentFields.Count > 0)
            TryAddDevice(currentFields, devices);

        return devices;
    }

    /// <summary>
    /// Asynchronously enumerates connected USB devices (non-blocking for UI callers).
    /// </summary>
    public static Task<List<USBDevice>> EnumerateDevicesAsync()
    {
        return Task.Run(EnumerateDevices);
    }

    private static void TryAddDevice(Dictionary<string, string> fields, List<USBDevice> devices)
    {
        string instanceId = GetFieldValue(fields, "InstanceId");
        if (string.IsNullOrEmpty(instanceId))
            return;

        Match match = VidPidRegex.Match(instanceId);
        if (!match.Success)
            return;

        string vid = match.Groups[1].Value.ToUpperInvariant();
        string pid = match.Groups[2].Value.ToUpperInvariant();
        string description = GetFieldValue(fields, "Description");
        string manufacturer = GetFieldValue(fields, "Manufacturer");
        string status = GetFieldValue(fields, "Status");

        devices.Add(new USBDevice(instanceId, vid, pid, description, manufacturer, status));
    }

    /// <summary>
    /// Disables (suspends) a device by its instance ID.
    /// Returns true if pnputil reports no error.
    /// </summary>
    public static bool DisableDevice(string instanceId)
    {
        PnpUtilResult result = RunPnpUtil("/disable-device", $"\"{instanceId}\"");
        return IsCommandSuccessful(result);
    }

    /// <summary>
    /// Enables (resumes) a device by its instance ID.
    /// Returns true if pnputil reports no error.
    /// </summary>
    public static bool EnableDevice(string instanceId)
    {
        PnpUtilResult result = RunPnpUtil("/enable-device", $"\"{instanceId}\"");
        return IsCommandSuccessful(result);
    }

    /// <summary>
    /// Re-enumerates devices and returns the current status of a specific device.
    /// </summary>
    public static string GetDeviceStatus(string instanceId)
    {
        List<USBDevice> devices = EnumerateDevices();
        USBDevice? device = devices.Find(d =>
            string.Equals(d.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        return device?.Status ?? "Unknown";
    }

    /// <summary>
    /// Checks if the given status string indicates the device is started/running.
    /// </summary>
    public static bool IsDeviceStarted(string status)
    {
        return Constants.StartedStatuses.Any(s =>
            string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the given status string indicates the device is disabled.
    /// </summary>
    public static bool IsDeviceDisabled(string status)
    {
        return Constants.DisabledStatuses.Any(s =>
            string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
    }
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
