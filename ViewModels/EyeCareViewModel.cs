using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComfortScreen.Models;
using ComfortScreen.Services;

namespace ComfortScreen.ViewModels;

public partial class EyeCareViewModel : ComfortScreenViewModelBase
{
    private bool _isUpdating;

    public EyeCareViewModel(ComfortScreenController controller)
        : base(controller)
    {
        RefreshFromState();
    }

    [ObservableProperty]
    private string _currentModeLabel = string.Empty;

    [ObservableProperty]
    private string _filterToggleText = string.Empty;

    [ObservableProperty]
    private bool _timedModeEnabled;

    [ObservableProperty]
    private string _dayStartText = string.Empty;

    [ObservableProperty]
    private string _nightStartText = string.Empty;

    [ObservableProperty]
    private bool _solarModeEnabled;

    [ObservableProperty]
    private string _latitudeText = string.Empty;

    [ObservableProperty]
    private string _longitudeText = string.Empty;

    [RelayCommand]
    private void ActivateDayMode()
    {
        Controller.SetModePreset(EyeMode.Day);
    }

    [RelayCommand]
    private void ActivateNightMode()
    {
        Controller.SetModePreset(EyeMode.Night);
    }

    [RelayCommand]
    private void ActivateReadingMode()
    {
        Controller.SetModePreset(EyeMode.Reading);
    }

    [RelayCommand]
    private void ToggleFilter()
    {
        Controller.ToggleFilterEnabled();
    }

    partial void OnTimedModeEnabledChanged(bool value)
    {
        PushScheduleChanges();
    }

    partial void OnDayStartTextChanged(string value)
    {
        PushScheduleChanges();
    }

    partial void OnNightStartTextChanged(string value)
    {
        PushScheduleChanges();
    }

    partial void OnSolarModeEnabledChanged(bool value)
    {
        PushScheduleChanges();
    }

    partial void OnLatitudeTextChanged(string value)
    {
        PushScheduleChanges();
    }

    partial void OnLongitudeTextChanged(string value)
    {
        PushScheduleChanges();
    }

    protected override void RefreshFromState()
    {
        _isUpdating = true;

        var settings = Controller.Settings;
        CurrentModeLabel = Controller.CurrentModeLabel;
        FilterToggleText = Controller.FilterToggleText;
        TimedModeEnabled = settings.TimedModeEnabled;
        DayStartText = settings.DayModeStart.ToString("HH\\:mm");
        NightStartText = settings.NightModeStart.ToString("HH\\:mm");
        SolarModeEnabled = settings.SolarModeEnabled;
        LatitudeText = settings.Latitude.ToString("0.####");
        LongitudeText = settings.Longitude.ToString("0.####");

        _isUpdating = false;
    }

    private void PushScheduleChanges()
    {
        if (_isUpdating)
        {
            return;
        }

        Controller.UpdateScheduleSettings(
            TimedModeEnabled,
            DayStartText,
            NightStartText,
            SolarModeEnabled,
            LatitudeText,
            LongitudeText
        );
    }
}
