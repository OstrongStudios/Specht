using Microsoft.Win32;

namespace Specht.App.Services;

/// <summary>
/// Manages the HKCU Run-key entry for autostart. Unpackaged-compatible.
/// </summary>
public static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Specht";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            var existing = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(existing);
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (key is null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return;
                key.SetValue(ValueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // ignore registry errors silently
        }
    }
}
