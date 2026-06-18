using ComfortScreen.Models;
using ComfortScreen.Views;
using Forms = System.Windows.Forms;

namespace ComfortScreen.Services;

public sealed class OverlayManager : Contracts.IOverlayService
{
    private readonly Dictionary<string, OverlayWindow> _windows = new(StringComparer.OrdinalIgnoreCase);

    public void Apply(AppSettings settings)
    {
        var screens = Forms.Screen.AllScreens;
        var configured = settings.MonitorSettings.ToDictionary(x => x.DeviceName, StringComparer.OrdinalIgnoreCase);

        var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var screen in screens)
        {
            activeKeys.Add(screen.DeviceName);
            var monitor = configured.GetValueOrDefault(screen.DeviceName) ?? new MonitorSetting
            {
                DeviceName = screen.DeviceName,
                Enabled = true,
                TemperatureKelvin = settings.GlobalTemperatureKelvin,
                BrightnessPercent = settings.GlobalBrightnessPercent,
                FilterOpacity = settings.GlobalFilterOpacity
            };

            var window = GetOrCreate(screen.DeviceName);
            window.UpdateBounds(screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height);

            if (!settings.FilterEnabled || !monitor.Enabled)
            {
                window.Hide();
                continue;
            }

            var color = settings.CurrentMode == EyeMode.Reading
                ? System.Windows.Media.Color.FromRgb(241, 230, 200)
                : ColorTemperatureConverter.ToColor(monitor.TemperatureKelvin);

            // Brightness is modeled as extra dimming to avoid relying on unsupported monitor APIs.
            var dimFactor = (100.0 - monitor.BrightnessPercent) / 100.0;
            var opacity = monitor.FilterOpacity + (dimFactor * 0.6);

            window.Apply(color, opacity);
            window.EnsureVisibleAndTopmost();
        }

        var staleKeys = _windows.Keys.Where(key => !activeKeys.Contains(key)).ToList();
        foreach (var key in staleKeys)
        {
            _windows[key].Close();
            _windows.Remove(key);
        }
    }

    public void HideAll()
    {
        foreach (var window in _windows.Values)
        {
            window.Hide();
        }
    }

    public void Dispose()
    {
        foreach (var window in _windows.Values)
        {
            window.Close();
        }

        _windows.Clear();
    }

    private OverlayWindow GetOrCreate(string key)
    {
        if (_windows.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new OverlayWindow();
        _windows.Add(key, created);
        return created;
    }
}
