using System.Drawing;

namespace USBSuspendTester;

/// <summary>
/// Application-wide constants: colors, defaults, pnputil arguments, multi-language keys.
/// </summary>
public static class Constants
{
    // Dark theme colors
    public static readonly Color BgColor = ColorTranslator.FromHtml("#1E1E1E");
    public static readonly Color PanelColor = ColorTranslator.FromHtml("#252526");
    public static readonly Color BorderColor = ColorTranslator.FromHtml("#3C3C3C");
    public static readonly Color TextColor = ColorTranslator.FromHtml("#D4D4D4");
    public static readonly Color TextDimColor = ColorTranslator.FromHtml("#808080");

    // Log colors
    public static readonly Color LogInfoColor = ColorTranslator.FromHtml("#569CD6");
    public static readonly Color LogPassColor = ColorTranslator.FromHtml("#4EC9B0");
    public static readonly Color LogFailColor = ColorTranslator.FromHtml("#F44747");
    public static readonly Color LogWarnColor = ColorTranslator.FromHtml("#DCDCAA");
    public static readonly Color LogSysColor = ColorTranslator.FromHtml("#C586C0");

    // Button colors
    public static readonly Color BtnStartBg = ColorTranslator.FromHtml("#0E639C");
    public static readonly Color BtnStopBg = ColorTranslator.FromHtml("#C72E2E");
    public static readonly Color BtnDefaultBg = ColorTranslator.FromHtml("#3C3C3C");
    public static readonly Color BtnTextColor = Color.White;

    // DataGridView colors
    public static readonly Color GridHeaderBg = ColorTranslator.FromHtml("#333333");
    public static readonly Color GridRowAltBg = ColorTranslator.FromHtml("#2A2A2A");
    public static readonly Color GridSelectionBg = ColorTranslator.FromHtml("#264F78");

    // pnputil process timeout
    public const int PnpUtilTimeoutMs = 30_000;

    // Default test parameters
    public const double DefaultSuspendDelay = 2.0;
    public const double DefaultResumeDelay = 2.0;
    public const double DefaultCycleInterval = 1.0;
    public const int DefaultMaxCycles = 0;

    // pnputil command arguments
    public const string PnpUtilPath = "pnputil";
    public static readonly string[] EnumDevicesArgs = ["/enum-devices", "/connected", "/class", "USB"];
    public static readonly string[] EnumAllDevicesArgs = ["/enum-devices", "/connected"];

    // Multi-language field key mappings (English + Chinese)
    public static readonly Dictionary<string, string[]> FieldKeys = new()
    {
        ["InstanceId"] = ["Instance ID", "\u5BE6\u9AD4\u8DEF\u5F91"],         // 實體路徑
        ["Description"] = ["Device Description", "\u88DD\u7F6E\u63CF\u8FF0"],  // 裝置描述
        ["ClassName"] = ["Class Name", "\u985E\u5225\u540D\u7A31"],            // 類別名稱
        ["ClassGuid"] = ["Class GUID", "\u985E\u5225 GUID"],
        ["Manufacturer"] = ["Manufacturer Name", "\u88FD\u9020\u5546\u540D\u7A31"], // 製造商名稱
        ["Status"] = ["Status", "\u72C0\u614B"],                               // 狀態
        ["DriverName"] = ["Driver Name", "\u9A45\u52D5\u7A0B\u5F0F\u540D\u7A31"],  // 驅動程式名稱
    };

    // Status values in multiple languages
    public static readonly string[] StartedStatuses = ["Started", "\u5DF2\u555F\u52D5"];    // 已啟動
    public static readonly string[] DisabledStatuses = ["Disabled", "\u5DF2\u505C\u7528"];  // 已停用

    // Form dimensions
    public const int FormWidth = 900;
    public const int FormHeight = 700;
    public const int FormMinWidth = 750;
    public const int FormMinHeight = 550;
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
