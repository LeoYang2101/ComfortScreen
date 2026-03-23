using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ComfortScreen.Contracts;
using ComfortScreen.Models;

namespace ComfortScreen.Services;

public sealed class ComfortScreenController : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IOverlayService _overlayService;
    private readonly IBrightnessService _brightnessService;
    private readonly IMonitorService _monitorService;
    private readonly IAppDialogService _appDialogService;
    private readonly IStartupService _startupService;
    private readonly IReminderDialogService _reminderDialogService;
    private readonly IBreakOverlayService _breakOverlayService;
    private readonly ITrayService _trayService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IAutoModeService _autoModeService;
    private readonly IThemeService _themeService;
    private readonly StartupLaunchContext _launchContext;

    private readonly DispatcherTimer _scheduleTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer _reminderTimer = new();
    private readonly DispatcherTimer _workRestTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _brightnessDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };

    private readonly Dictionary<string, MonitorSetting> _monitorMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<MonitorTargetItem> _monitorTargets = new();

    private Window? _shellWindow;
    private CancellationTokenSource? _breakCts;
    private bool _workRestRunning;
    private bool _isRestPhase;
    private bool _initialized;
    private TimeSpan _phaseDuration;
    private TimeSpan _phaseRemaining;

    public ComfortScreenController(
        ISettingsService settingsService,
        IOverlayService overlayService,
        IBrightnessService brightnessService,
        IMonitorService monitorService,
        IAppDialogService appDialogService,
        IStartupService startupService,
        IReminderDialogService reminderDialogService,
        IBreakOverlayService breakOverlayService,
        ITrayService trayService,
        IHotkeyService hotkeyService,
        IAutoModeService autoModeService,
        IThemeService themeService,
        StartupLaunchContext launchContext
    )
    {
        _settingsService = settingsService;
        _overlayService = overlayService;
        _brightnessService = brightnessService;
        _monitorService = monitorService;
        _appDialogService = appDialogService;
        _startupService = startupService;
        _reminderDialogService = reminderDialogService;
        _breakOverlayService = breakOverlayService;
        _trayService = trayService;
        _hotkeyService = hotkeyService;
        _autoModeService = autoModeService;
        _themeService = themeService;
        _launchContext = launchContext;

        HotkeyModifierOptions = new ReadOnlyCollection<HotkeyModifierOption>(
            new[]
            {
                new HotkeyModifierOption("Ctrl + Alt", 0x0002 | 0x0001),
                new HotkeyModifierOption("Ctrl + Shift", 0x0002 | 0x0004),
                new HotkeyModifierOption("Alt + Shift", 0x0001 | 0x0004),
                new HotkeyModifierOption("Ctrl + Alt + Shift", 0x0002 | 0x0001 | 0x0004)
            }
        );

        HotkeyKeys = new ReadOnlyCollection<Key>(new[] { Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12 });

        Settings = _settingsService.Load();

        _scheduleTimer.Tick += (_, _) => ApplyAutoModeRules();
        _reminderTimer.Tick += (_, _) => ShowReminder();
        _workRestTimer.Tick += (_, _) => OnWorkRestTick();
        _brightnessDebounceTimer.Tick += OnBrightnessDebounceTick;

        LoadMonitorSettingsMap();
        RefreshMonitorsInternal();
        UpdateWorkRestStatus();
        HotkeyStatusText = "全局快捷键用于快速开关护眼模式";
    }

    public event EventHandler? StateChanged;

    public event EventHandler? RestoreRequested;

    public event EventHandler? ExitRequested;

    public AppSettings Settings { get; private set; }

    public IReadOnlyList<MonitorTargetItem> MonitorTargets => _monitorTargets;

    public IReadOnlyList<HotkeyModifierOption> HotkeyModifierOptions { get; }

    public IReadOnlyList<Key> HotkeyKeys { get; }

    public string HotkeyStatusText { get; private set; } = string.Empty;

    public string CountdownStatusText { get; private set; } = "工作/休息计时器未启动";

    public double WorkRestProgress { get; private set; }

    public bool WorkRestRunning => _workRestRunning;

    public string WorkRestButtonText => _workRestRunning ? "暂停循环" : "开始循环";

    public string CurrentModeLabel => GetModeLabel(Settings.CurrentMode);

    public string FilterToggleText => Settings.FilterEnabled ? "关闭护眼滤镜" : "开启护眼滤镜";

    public string TrayText =>
        $"{CurrentModeLabel} | {Settings.GlobalTemperatureKelvin:0}K | {Settings.GlobalBrightnessPercent:0}%";

    public void Initialize(Window shellWindow)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _shellWindow = shellWindow;

        _themeService.ApplyTheme(Settings.ThemeMode, shellWindow);

        _trayService.Initialize(
            restoreAction: () => System.Windows.Application.Current.Dispatcher.Invoke(() => RestoreRequested?.Invoke(this, EventArgs.Empty)),
            toggleFilterAction: () => System.Windows.Application.Current.Dispatcher.Invoke(ToggleFilterEnabled),
            quickNightModeAction: () => System.Windows.Application.Current.Dispatcher.Invoke(() => SetModePreset(EyeMode.Night)),
            exitAction: () => System.Windows.Application.Current.Dispatcher.Invoke(() => ExitRequested?.Invoke(this, EventArgs.Empty))
        );

        _hotkeyService.Initialize(shellWindow);
        RebindHotkey();

        _scheduleTimer.Start();
        RestartReminderTimer();
        ApplyCurrentState();

        if (Settings.FirstRunGuidePending && !_launchContext.IsStartupLaunch)
        {
            ShowFirstRunGuide();
            Settings.FirstRunGuidePending = false;
            PersistSettings();
        }

        NotifyStateChanged();
    }

    public void Shutdown()
    {
        _breakCts?.Cancel();
        _scheduleTimer.Stop();
        _reminderTimer.Stop();
        _workRestTimer.Stop();
        _brightnessDebounceTimer.Stop();
        _overlayService.HideAll();
        _trayService.Dispose();
        _hotkeyService.Dispose();

        if (_overlayService is IDisposable disposableOverlay)
        {
            disposableOverlay.Dispose();
        }
    }

    public void ToggleFilterEnabled()
    {
        Settings.FilterEnabled = !Settings.FilterEnabled;
        PersistAndApply(debounceBrightness: false);
    }

    public void SetModePreset(EyeMode mode)
    {
        var (temperature, brightness, opacity) = mode switch
        {
            EyeMode.Day => (5600d, 88d, 0.12d),
            EyeMode.Night => (3000d, 58d, 0.34d),
            EyeMode.Reading => (4300d, 72d, 0.24d),
            _ => (
                Settings.GlobalTemperatureKelvin,
                Settings.GlobalBrightnessPercent,
                Settings.GlobalFilterOpacity
            )
        };

        Settings.CurrentMode = mode;
        Settings.GlobalTemperatureKelvin = temperature;
        Settings.GlobalBrightnessPercent = brightness;
        Settings.GlobalFilterOpacity = opacity;

        foreach (var monitor in _monitorMap.Values)
        {
            monitor.TemperatureKelvin = temperature;
            monitor.BrightnessPercent = brightness;
            monitor.FilterOpacity = opacity;
        }

        PersistAndApply();
    }

    public void RefreshMonitors()
    {
        RefreshMonitorsInternal();
        PersistAndApply();
    }

    public MonitorSetting GetMonitorSetting(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName) || deviceName == "*")
        {
            return new MonitorSetting
            {
                DeviceName = "*",
                Enabled = true,
                TemperatureKelvin = Settings.GlobalTemperatureKelvin,
                BrightnessPercent = Settings.GlobalBrightnessPercent,
                FilterOpacity = Settings.GlobalFilterOpacity
            };
        }

        if (_monitorMap.TryGetValue(deviceName, out var monitor))
        {
            return new MonitorSetting
            {
                DeviceName = monitor.DeviceName,
                Enabled = monitor.Enabled,
                TemperatureKelvin = monitor.TemperatureKelvin,
                BrightnessPercent = monitor.BrightnessPercent,
                FilterOpacity = monitor.FilterOpacity
            };
        }

        return GetMonitorSetting("*");
    }

    public void ApplyMonitorValues(string? deviceName, double temperature, double brightness, double opacityPercent)
    {
        var normalizedOpacity = Math.Clamp(opacityPercent / 100.0, 0, 0.8);

        if (string.IsNullOrWhiteSpace(deviceName) || deviceName == "*")
        {
            Settings.GlobalTemperatureKelvin = temperature;
            Settings.GlobalBrightnessPercent = brightness;
            Settings.GlobalFilterOpacity = normalizedOpacity;

            foreach (var monitor in _monitorMap.Values)
            {
                monitor.TemperatureKelvin = temperature;
                monitor.BrightnessPercent = brightness;
                monitor.FilterOpacity = normalizedOpacity;
            }
        }
        else if (_monitorMap.TryGetValue(deviceName, out var monitor))
        {
            monitor.TemperatureKelvin = temperature;
            monitor.BrightnessPercent = brightness;
            monitor.FilterOpacity = normalizedOpacity;
        }

        Settings.CurrentMode = EyeMode.Custom;
        PersistAndApply();
    }

    public void UpdateScheduleSettings(
        bool timedModeEnabled,
        string dayStartText,
        string nightStartText,
        bool solarModeEnabled,
        string latitudeText,
        string longitudeText
    )
    {
        Settings.TimedModeEnabled = timedModeEnabled;
        Settings.SolarModeEnabled = solarModeEnabled;

        if (TimeOnly.TryParse(dayStartText, out var dayStart))
        {
            Settings.DayModeStart = dayStart;
        }

        if (TimeOnly.TryParse(nightStartText, out var nightStart))
        {
            Settings.NightModeStart = nightStart;
        }

        Settings.Latitude = ParseDouble(latitudeText, Settings.Latitude, -90, 90);
        Settings.Longitude = ParseDouble(longitudeText, Settings.Longitude, -180, 180);

        PersistAndApply(debounceBrightness: false);
    }

    public void UpdateReminderSettings(bool enabled, string intervalText, string holdText)
    {
        Settings.ReminderEnabled = enabled;
        Settings.ReminderIntervalMinutes = ParseInt(intervalText, Settings.ReminderIntervalMinutes, 1, 240);
        Settings.ReminderHoldSeconds = ParseInt(holdText, Settings.ReminderHoldSeconds, 5, 90);

        RestartReminderTimer();
        PersistAndApply(debounceBrightness: false);
    }

    public void ShowReminder()
    {
        _reminderDialogService.Show(Settings.ReminderHoldSeconds);
    }

    public void UpdateWorkRestSettings(
        bool enabled,
        string workMinutesText,
        string restMinutesText,
        bool forceBreak,
        bool allowExitBreakOverlay
    )
    {
        Settings.WorkRestEnabled = enabled;
        Settings.WorkMinutes = ParseInt(workMinutesText, Settings.WorkMinutes, 1, 180);
        Settings.RestMinutes = ParseInt(restMinutesText, Settings.RestMinutes, 1, 120);
        Settings.ForceBreak = forceBreak;
        Settings.AllowBreakOverlayExit = allowExitBreakOverlay;

        if (!Settings.WorkRestEnabled)
        {
            _workRestRunning = false;
            _workRestTimer.Stop();
            _breakCts?.Cancel();
            _phaseDuration = TimeSpan.Zero;
            _phaseRemaining = TimeSpan.Zero;
        }

        UpdateWorkRestStatus();
        PersistAndApply(debounceBrightness: false);
    }

    public void ToggleWorkRestRunning()
    {
        if (!Settings.WorkRestEnabled)
        {
            Settings.WorkRestEnabled = true;
        }

        _workRestRunning = !_workRestRunning;

        if (_workRestRunning)
        {
            if (_phaseDuration == TimeSpan.Zero)
            {
                BeginWorkPhase();
            }

            _workRestTimer.Start();
        }
        else
        {
            _workRestTimer.Stop();
            _breakCts?.Cancel();
        }

        UpdateWorkRestStatus();
        PersistAndApply(debounceBrightness: false);
    }

    public void UpdateHotkeySettings(bool enabled, int modifiers, Key key)
    {
        Settings.Hotkey.Enabled = enabled;
        Settings.Hotkey.Modifiers = modifiers;
        Settings.Hotkey.VirtualKey = KeyInterop.VirtualKeyFromKey(key);

        RebindHotkey();
        PersistAndApply(debounceBrightness: false);
    }

    public void UpdateTheme(AppThemeMode themeMode)
    {
        Settings.ThemeMode = themeMode;

        if (_shellWindow is not null)
        {
            _themeService.ApplyTheme(themeMode, _shellWindow);
        }

        PersistAndApply(debounceBrightness: false);
    }

    public void UpdateStartupSetting(bool enabled)
    {
        var previous = Settings.StartWithWindows;
        if (previous == enabled)
        {
            return;
        }

        try
        {
            _startupService.SetEnabled(enabled);

            Settings.StartWithWindows = _startupService.IsEnabled();
            if (Settings.StartWithWindows != enabled)
            {
                throw new InvalidOperationException("Windows 启动项状态未按预期更新。");
            }

            PersistSettings();
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Settings.StartWithWindows = previous;

            try
            {
                _startupService.SetEnabled(previous);
            }
            catch
            {
                // Ignore rollback errors and keep the UI consistent with the last known setting.
            }

            NotifyStateChanged();
            _appDialogService.ShowError(
                "开机自启设置失败",
                $"无法更新 Windows 启动项：{ex.Message}",
                _shellWindow
            );
        }
    }

    private void ApplyCurrentState()
    {
        SyncMonitorSettingsToModel();
        _overlayService.Apply(Settings);
        _trayService.UpdateText(TrayText);
        DebounceHardwareBrightness();
    }

    private void PersistAndApply(bool debounceBrightness = true)
    {
        SyncMonitorSettingsToModel();
        PersistSettings();
        _overlayService.Apply(Settings);
        _trayService.UpdateText(TrayText);

        if (debounceBrightness)
        {
            DebounceHardwareBrightness();
        }

        NotifyStateChanged();
    }

    private void PersistSettings()
    {
        _settingsService.Save(Settings);
    }

    private void LoadMonitorSettingsMap()
    {
        _monitorMap.Clear();

        foreach (var item in Settings.MonitorSettings)
        {
            _monitorMap[item.DeviceName] = item;
        }
    }

    private void RefreshMonitorsInternal()
    {
        var targets = _monitorService.GetTargets();
        _monitorTargets.Clear();

        foreach (var target in targets)
        {
            _monitorTargets.Add(target);

            if (target.IsAll || _monitorMap.ContainsKey(target.DeviceName))
            {
                continue;
            }

            _monitorMap[target.DeviceName] = new MonitorSetting
            {
                DeviceName = target.DeviceName,
                Enabled = true,
                TemperatureKelvin = Settings.GlobalTemperatureKelvin,
                BrightnessPercent = Settings.GlobalBrightnessPercent,
                FilterOpacity = Settings.GlobalFilterOpacity
            };
        }

        var activeKeys = targets.Where(x => !x.IsAll).Select(x => x.DeviceName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var staleKeys = _monitorMap.Keys.Where(key => !activeKeys.Contains(key)).ToList();

        foreach (var key in staleKeys)
        {
            _monitorMap.Remove(key);
        }

        SyncMonitorSettingsToModel();
    }

    private void SyncMonitorSettingsToModel()
    {
        Settings.MonitorSettings = _monitorMap.Values
            .OrderBy(x => x.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ApplyAutoModeRules()
    {
        if (!Settings.TimedModeEnabled && !Settings.SolarModeEnabled)
        {
            return;
        }

        var desired = _autoModeService.DetermineMode(Settings, DateTime.Now);
        if (desired.HasValue && desired.Value != Settings.CurrentMode)
        {
            SetModePreset(desired.Value);
        }
    }

    private void RestartReminderTimer()
    {
        _reminderTimer.Stop();

        if (!Settings.ReminderEnabled)
        {
            return;
        }

        _reminderTimer.Interval = TimeSpan.FromMinutes(Math.Clamp(Settings.ReminderIntervalMinutes, 1, 240));
        _reminderTimer.Start();
    }

    private void OnWorkRestTick()
    {
        if (!_workRestRunning)
        {
            return;
        }

        _phaseRemaining -= TimeSpan.FromSeconds(1);
        if (_phaseRemaining < TimeSpan.Zero)
        {
            _phaseRemaining = TimeSpan.Zero;
        }

        UpdateWorkRestStatus();

        if (_phaseRemaining > TimeSpan.Zero)
        {
            NotifyStateChanged();
            return;
        }

        if (_isRestPhase)
        {
            BeginWorkPhase();
        }
        else
        {
            BeginRestPhase();
        }

        NotifyStateChanged();
    }

    private void BeginWorkPhase()
    {
        _isRestPhase = false;
        _phaseDuration = TimeSpan.FromMinutes(Math.Clamp(Settings.WorkMinutes, 1, 180));
        _phaseRemaining = _phaseDuration;
        UpdateWorkRestStatus();
    }

    private void BeginRestPhase()
    {
        _isRestPhase = true;
        _phaseDuration = TimeSpan.FromMinutes(Math.Clamp(Settings.RestMinutes, 1, 120));
        _phaseRemaining = _phaseDuration;
        UpdateWorkRestStatus();

        _breakCts?.Cancel();
        _breakCts = new CancellationTokenSource();

        if (Settings.ForceBreak)
        {
            _ = _breakOverlayService.ShowAsync(_phaseDuration, Settings.AllowBreakOverlayExit, _breakCts.Token);
        }
    }

    private void UpdateWorkRestStatus()
    {
        if (!_workRestRunning)
        {
            CountdownStatusText = "工作/休息计时器未启动";
            WorkRestProgress = 0;
            return;
        }

        var phaseName = _isRestPhase ? "休息期" : "工作期";
        CountdownStatusText = $"{phaseName} 剩余 {_phaseRemaining:mm\\:ss}";

        var elapsed = _phaseDuration - _phaseRemaining;
        WorkRestProgress = _phaseDuration.TotalSeconds <= 0
            ? 0
            : Math.Clamp(elapsed.TotalSeconds / _phaseDuration.TotalSeconds * 100, 0, 100);
    }

    private void DebounceHardwareBrightness()
    {
        _brightnessDebounceTimer.Stop();
        _brightnessDebounceTimer.Start();
    }

    private async void OnBrightnessDebounceTick(object? sender, EventArgs e)
    {
        _brightnessDebounceTimer.Stop();

        if (!Settings.FilterEnabled)
        {
            return;
        }

        await _brightnessService.TrySetBrightnessAsync((int)Math.Round(Settings.GlobalBrightnessPercent));
    }

    private void RebindHotkey()
    {
        _hotkeyService.Unregister();

        if (_shellWindow is null)
        {
            HotkeyStatusText = "主窗口尚未就绪";
            NotifyStateChanged();
            return;
        }

        if (!Settings.Hotkey.Enabled)
        {
            HotkeyStatusText = "全局快捷键已关闭";
            NotifyStateChanged();
            return;
        }

        var success = _hotkeyService.Register(Settings.Hotkey, OnHotkeyPressed);
        HotkeyStatusText = success ? "全局快捷键已生效" : "快捷键注册失败，可能被占用";
        NotifyStateChanged();
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        ToggleFilterEnabled();
    }

    private void ShowFirstRunGuide()
    {
        _appDialogService.ShowInfo(
            "首次使用引导",
            "欢迎使用 ComfortScreen。\n\n1. 先从日间/夜间/阅读模式中选一个。\n2. 用色温、亮度、透明度滑条做微调。\n3. 开启 20-20-20 和工作/休息提醒。\n4. 可用托盘图标和全局快捷键快速开关护眼。",
            _shellWindow
        );
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string GetModeLabel(EyeMode mode)
    {
        return mode switch
        {
            EyeMode.Day => "日间模式",
            EyeMode.Night => "夜间模式",
            EyeMode.Reading => "阅读模式",
            _ => "自定义模式"
        };
    }

    private static int ParseInt(string text, int fallback, int min, int max)
    {
        return int.TryParse(text, out var parsed) ? Math.Clamp(parsed, min, max) : fallback;
    }

    private static double ParseDouble(string text, double fallback, double min, double max)
    {
        return double.TryParse(text, out var parsed) ? Math.Clamp(parsed, min, max) : fallback;
    }

    public void Dispose()
    {
        Shutdown();
    }
}
