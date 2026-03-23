using System.Text.Json.Serialization;

namespace ComfortScreen.Models;

public enum EyeMode
{
    Day,
    Night,
    Reading,
    Custom
}

public enum ThemeSkin
{
    Simple,
    Transparent,
    Frosted
}

public enum AppThemeMode
{
    Light,
    Dark
}

public sealed class HotkeySettings
{
    public bool Enabled { get; set; } = true;

    public int Modifiers { get; set; } = 3; // Ctrl + Alt

    public int VirtualKey { get; set; } = 121; // F10
}

public sealed class MonitorSetting
{
    public string DeviceName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public double TemperatureKelvin { get; set; } = 4500;

    public double BrightnessPercent { get; set; } = 70;

    public double FilterOpacity { get; set; } = 0.25;
}

public sealed class AppSettings
{
    public bool FirstRunGuidePending { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EyeMode CurrentMode { get; set; } = EyeMode.Day;

    public bool FilterEnabled { get; set; } = true;

    public double GlobalTemperatureKelvin { get; set; } = 5000;

    public double GlobalBrightnessPercent { get; set; } = 75;

    public double GlobalFilterOpacity { get; set; } = 0.20;

    public bool TimedModeEnabled { get; set; }

    public TimeOnly DayModeStart { get; set; } = new(7, 0);

    public TimeOnly NightModeStart { get; set; } = new(22, 0);

    public bool SolarModeEnabled { get; set; }

    public double Latitude { get; set; } = 31.2304;

    public double Longitude { get; set; } = 121.4737;

    public bool ReminderEnabled { get; set; } = true;

    public int ReminderIntervalMinutes { get; set; } = 20;

    public int ReminderHoldSeconds { get; set; } = 20;

    public bool WorkRestEnabled { get; set; }

    public int WorkMinutes { get; set; } = 50;

    public int RestMinutes { get; set; } = 10;

    public bool ForceBreak { get; set; } = true;

    public bool AllowBreakOverlayExit { get; set; } = false;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Light;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThemeSkin Theme { get; set; } = ThemeSkin.Simple;

    public bool StartWithWindows { get; set; }

    public HotkeySettings Hotkey { get; set; } = new();

    public List<MonitorSetting> MonitorSettings { get; set; } = new();
}

