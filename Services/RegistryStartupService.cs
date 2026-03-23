using ComfortScreen.Contracts;
using Microsoft.Win32;

namespace ComfortScreen.Services;

public sealed class RegistryStartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ComfortScreen";
    private const string StartupArgument = "--startup";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(ValueName) as string;
        return string.Equals(Normalize(value), Normalize(GetCommandLine()), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("无法访问当前用户的启动项注册表。");

        if (enabled)
        {
            key.SetValue(ValueName, GetCommandLine(), RegistryValueKind.String);
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public string GetCommandLine()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("无法解析当前应用的可执行文件路径。");
        }

        return $"\"{processPath}\" {StartupArgument}";
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
