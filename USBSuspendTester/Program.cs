using System.Diagnostics;
using System.Security.Principal;
using USBSuspendTester.UI;

namespace USBSuspendTester;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Check if running as administrator
        if (!IsAdministrator())
        {
            // Re-launch with elevated privileges
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath ?? Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas",
                };
                Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(
                    "This application requires administrator privileges to control USB devices.\n" +
                    "Please run as administrator.",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
