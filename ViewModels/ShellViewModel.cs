using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComfortScreen.Services;

namespace ComfortScreen.ViewModels;

public partial class ShellViewModel : ComfortScreenViewModelBase
{
    public ShellViewModel(ComfortScreenController controller)
        : base(controller)
    {
        RefreshFromState();
    }

    public event EventHandler? MinimizeRequested;

    [ObservableProperty]
    private string _currentModeText = string.Empty;

    [ObservableProperty]
    private string _filterToggleText = string.Empty;

    [ObservableProperty]
    private string _countdownStatusText = string.Empty;

    [RelayCommand]
    private void ToggleFilter()
    {
        Controller.ToggleFilterEnabled();
    }

    [RelayCommand]
    private void MinimizeToTray()
    {
        MinimizeRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void RefreshFromState()
    {
        CurrentModeText = $"当前模式：{Controller.CurrentModeLabel}";
        FilterToggleText = Controller.FilterToggleText;
        CountdownStatusText = Controller.CountdownStatusText;
    }
}
