using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComfortScreen.Models;
using ComfortScreen.Services;

namespace ComfortScreen.ViewModels;

public partial class DisplayViewModel : ComfortScreenViewModelBase
{
    private bool _isUpdating;

    public DisplayViewModel(ComfortScreenController controller)
        : base(controller)
    {
        RefreshFromState();
    }

    public ObservableCollection<MonitorTargetItem> MonitorTargets { get; } = new();

    [ObservableProperty]
    private MonitorTargetItem? _selectedMonitorTarget;

    [ObservableProperty]
    private double _temperatureKelvin;

    [ObservableProperty]
    private double _brightnessPercent;

    [ObservableProperty]
    private double _filterOpacityPercent;

    [ObservableProperty]
    private string _temperatureLabel = string.Empty;

    [ObservableProperty]
    private string _brightnessLabel = string.Empty;

    [ObservableProperty]
    private string _opacityLabel = string.Empty;

    [RelayCommand]
    private void RefreshMonitors()
    {
        Controller.RefreshMonitors();
    }

    partial void OnSelectedMonitorTargetChanged(MonitorTargetItem? value)
    {
        if (_isUpdating)
        {
            return;
        }

        RefreshSelectedMonitorValues();
    }

    partial void OnTemperatureKelvinChanged(double value)
    {
        TemperatureLabel = $"色温：{value:0}K";
        PushMonitorChanges();
    }

    partial void OnBrightnessPercentChanged(double value)
    {
        BrightnessLabel = $"亮度：{value:0}%";
        PushMonitorChanges();
    }

    partial void OnFilterOpacityPercentChanged(double value)
    {
        OpacityLabel = $"滤镜透明度：{value:0}%";
        PushMonitorChanges();
    }

    protected override void RefreshFromState()
    {
        _isUpdating = true;

        var selectedDeviceName = SelectedMonitorTarget?.DeviceName ?? "*";

        MonitorTargets.Clear();
        foreach (var target in Controller.MonitorTargets)
        {
            MonitorTargets.Add(target);
        }

        SelectedMonitorTarget = MonitorTargets.FirstOrDefault(x => x.DeviceName == selectedDeviceName)
            ?? MonitorTargets.FirstOrDefault();

        RefreshSelectedMonitorValues();

        _isUpdating = false;
    }

    private void RefreshSelectedMonitorValues()
    {
        var setting = Controller.GetMonitorSetting(SelectedMonitorTarget?.DeviceName);

        _isUpdating = true;
        TemperatureKelvin = setting.TemperatureKelvin;
        BrightnessPercent = setting.BrightnessPercent;
        FilterOpacityPercent = setting.FilterOpacity * 100;
        TemperatureLabel = $"色温：{TemperatureKelvin:0}K";
        BrightnessLabel = $"亮度：{BrightnessPercent:0}%";
        OpacityLabel = $"滤镜透明度：{FilterOpacityPercent:0}%";
        _isUpdating = false;
    }

    private void PushMonitorChanges()
    {
        if (_isUpdating)
        {
            return;
        }

        Controller.ApplyMonitorValues(
            SelectedMonitorTarget?.DeviceName,
            TemperatureKelvin,
            BrightnessPercent,
            FilterOpacityPercent
        );
    }
}
