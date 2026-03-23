using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComfortScreen.Services;

namespace ComfortScreen.ViewModels;

public partial class DashboardViewModel : ComfortScreenViewModelBase
{
    public DashboardViewModel(ComfortScreenController controller)
        : base(controller)
    {
        RefreshFromState();
    }

    [ObservableProperty]
    private string _modeSummary = string.Empty;

    [ObservableProperty]
    private string _displaySummary = string.Empty;

    [ObservableProperty]
    private string _reminderSummary = string.Empty;

    [ObservableProperty]
    private string _automationSummary = string.Empty;

    [ObservableProperty]
    private string _countdownStatusText = string.Empty;

    [ObservableProperty]
    private double _workRestProgress;

    [ObservableProperty]
    private string _filterToggleText = string.Empty;

    [RelayCommand]
    private void ActivateDayMode()
    {
        Controller.SetModePreset(Models.EyeMode.Day);
    }

    [RelayCommand]
    private void ActivateNightMode()
    {
        Controller.SetModePreset(Models.EyeMode.Night);
    }

    [RelayCommand]
    private void ActivateReadingMode()
    {
        Controller.SetModePreset(Models.EyeMode.Reading);
    }

    [RelayCommand]
    private void ToggleFilter()
    {
        Controller.ToggleFilterEnabled();
    }

    protected override void RefreshFromState()
    {
        var settings = Controller.Settings;

        ModeSummary =
            $"{Controller.CurrentModeLabel} · 色温 {settings.GlobalTemperatureKelvin:0}K · 亮度 {settings.GlobalBrightnessPercent:0}% · 透明度 {settings.GlobalFilterOpacity * 100:0}%";
        DisplaySummary = $"已识别 {Controller.MonitorTargets.Count - 1} 台显示器，支持“全部显示器”联动调节";
        ReminderSummary =
            settings.ReminderEnabled
                ? $"20-20-20 已开启，每 {settings.ReminderIntervalMinutes} 分钟提醒一次，保持 {settings.ReminderHoldSeconds} 秒"
                : "20-20-20 提醒已关闭";
        AutomationSummary =
            settings.SolarModeEnabled
                ? $"日出日落自动切换已开启，经纬度 {settings.Latitude:0.####}, {settings.Longitude:0.####}"
                : settings.TimedModeEnabled
                    ? $"定时切换已开启，日间 {settings.DayModeStart:HH\\:mm}，夜间 {settings.NightModeStart:HH\\:mm}"
                    : "自动切换当前未开启";
        CountdownStatusText = Controller.CountdownStatusText;
        WorkRestProgress = Controller.WorkRestProgress;
        FilterToggleText = Controller.FilterToggleText;
    }
}
