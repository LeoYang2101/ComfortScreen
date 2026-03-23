using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComfortScreen.Services;

namespace ComfortScreen.ViewModels;

public partial class ReminderViewModel : ComfortScreenViewModelBase
{
    private bool _isUpdating;

    public ReminderViewModel(ComfortScreenController controller)
        : base(controller)
    {
        RefreshFromState();
    }

    [ObservableProperty]
    private bool _reminderEnabled;

    [ObservableProperty]
    private string _reminderIntervalText = string.Empty;

    [ObservableProperty]
    private string _reminderHoldText = string.Empty;

    [ObservableProperty]
    private bool _workRestEnabled;

    [ObservableProperty]
    private string _workMinutesText = string.Empty;

    [ObservableProperty]
    private string _restMinutesText = string.Empty;

    [ObservableProperty]
    private bool _forceBreak;

    [ObservableProperty]
    private bool _allowBreakOverlayExit;

    [ObservableProperty]
    private string _countdownStatusText = string.Empty;

    [ObservableProperty]
    private double _workRestProgress;

    [ObservableProperty]
    private string _workRestButtonText = string.Empty;

    [RelayCommand]
    private void TestReminder()
    {
        Controller.ShowReminder();
    }

    [RelayCommand]
    private void ToggleWorkRest()
    {
        Controller.ToggleWorkRestRunning();
    }

    partial void OnReminderEnabledChanged(bool value)
    {
        PushReminderChanges();
    }

    partial void OnReminderIntervalTextChanged(string value)
    {
        PushReminderChanges();
    }

    partial void OnReminderHoldTextChanged(string value)
    {
        PushReminderChanges();
    }

    partial void OnWorkRestEnabledChanged(bool value)
    {
        PushWorkRestChanges();
    }

    partial void OnWorkMinutesTextChanged(string value)
    {
        PushWorkRestChanges();
    }

    partial void OnRestMinutesTextChanged(string value)
    {
        PushWorkRestChanges();
    }

    partial void OnForceBreakChanged(bool value)
    {
        PushWorkRestChanges();
    }

    partial void OnAllowBreakOverlayExitChanged(bool value)
    {
        PushWorkRestChanges();
    }

    protected override void RefreshFromState()
    {
        _isUpdating = true;

        var settings = Controller.Settings;

        ReminderEnabled = settings.ReminderEnabled;
        ReminderIntervalText = settings.ReminderIntervalMinutes.ToString();
        ReminderHoldText = settings.ReminderHoldSeconds.ToString();
        WorkRestEnabled = settings.WorkRestEnabled;
        WorkMinutesText = settings.WorkMinutes.ToString();
        RestMinutesText = settings.RestMinutes.ToString();
        ForceBreak = settings.ForceBreak;
        AllowBreakOverlayExit = settings.AllowBreakOverlayExit;
        CountdownStatusText = Controller.CountdownStatusText;
        WorkRestProgress = Controller.WorkRestProgress;
        WorkRestButtonText = Controller.WorkRestButtonText;

        _isUpdating = false;
    }

    private void PushReminderChanges()
    {
        if (_isUpdating)
        {
            return;
        }

        Controller.UpdateReminderSettings(ReminderEnabled, ReminderIntervalText, ReminderHoldText);
    }

    private void PushWorkRestChanges()
    {
        if (_isUpdating)
        {
            return;
        }

        Controller.UpdateWorkRestSettings(
            WorkRestEnabled,
            WorkMinutesText,
            RestMinutesText,
            ForceBreak,
            AllowBreakOverlayExit
        );
    }
}
