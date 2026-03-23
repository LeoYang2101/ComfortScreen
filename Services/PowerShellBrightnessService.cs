using System.Diagnostics;
using System.Text;
using ComfortScreen.Contracts;

namespace ComfortScreen.Services;

public sealed class PowerShellBrightnessService : IBrightnessService
{
    public async Task<bool> TrySetBrightnessAsync(int brightness, CancellationToken cancellationToken = default)
    {
        var safeBrightness = Math.Clamp(brightness, 1, 100);
        var command = $"Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightnessMethods | ForEach-Object {{ $_.WmiSetBrightness(1,{safeBrightness}) }} | Out-Null";
        return await RunPowerShellAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> RunPowerShellAsync(string script, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            if (!process.Start())
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
