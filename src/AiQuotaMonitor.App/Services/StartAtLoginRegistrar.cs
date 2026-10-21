using Microsoft.Win32;

namespace AiQuotaMonitor.App.Services;

public static class StartAtLoginRegistrar
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AiQuotaMonitor";

    public static void Apply(bool enabled, string? executablePath = null)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("HKCU Run key is unavailable.");

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var path = executablePath ?? Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Application executable path is unavailable.");
        }

        key.SetValue(ValueName, $"\"{path}\" --minimized", RegistryValueKind.String);
    }
}
