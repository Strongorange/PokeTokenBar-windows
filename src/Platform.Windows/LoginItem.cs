using Microsoft.Win32;

namespace PokeTokenBar.Platform.Windows;

/// <summary>
/// Launch-at-login via the per-user Run key (HKCU, no admin required) — the
/// Windows equivalent of the macOS SMAppService login item. Registry access is
/// delegate-injected so the command encoding/comparison logic is unit-testable
/// without touching the real registry.
/// </summary>
public static class LoginItem
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "PokeTokenBar";

    public static string EncodeCommand(string exePath) => $"\"{exePath.Trim()}\"";

    public static bool MatchesCommand(string? command, string exePath) =>
        string.Equals(command?.Trim(), EncodeCommand(exePath), StringComparison.OrdinalIgnoreCase);

    public static bool IsEnabled(string exePath, Func<string, string?>? read = null)
    {
        read ??= DefaultRead;
        return MatchesCommand(read(ValueName), exePath);
    }

    public static void SetEnabled(bool enabled, string exePath, Action<string, string?>? write = null)
    {
        write ??= DefaultWrite;
        write(ValueName, enabled ? EncodeCommand(exePath) : null);
    }

    private static string? DefaultRead(string name)
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(name) as string;
    }

    private static void DefaultWrite(string name, string? value)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (value is null) key.DeleteValue(name, throwOnMissingValue: false);
        else key.SetValue(name, value);
    }
}
