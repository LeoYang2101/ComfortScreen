using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ComfortScreen.Models;
using ComfortScreen.Services;

namespace ComfortScreen.ViewModels;

public partial class SettingsViewModel : ComfortScreenViewModelBase
{
    private bool _isUpdating;

    public SettingsViewModel(ComfortScreenController controller)
        : base(controller)
    {
        foreach (var option in controller.HotkeyModifierOptions)
        {
            HotkeyModifierOptions.Add(option);
        }

        foreach (var key in controller.HotkeyKeys)
        {
            HotkeyKeys.Add(key);
        }

        ThemeOptions.Add(new ThemeModeOption("浅色", AppThemeMode.Light));
        ThemeOptions.Add(new ThemeModeOption("暗色", AppThemeMode.Dark));

        RefreshFromState();
    }

    public ObservableCollection<HotkeyModifierOption> HotkeyModifierOptions { get; } = new();

    public ObservableCollection<Key> HotkeyKeys { get; } = new();

    public ObservableCollection<ThemeModeOption> ThemeOptions { get; } = new();

    [ObservableProperty]
    private bool _hotkeyEnabled;

    [ObservableProperty]
    private HotkeyModifierOption? _selectedHotkeyModifier;

    [ObservableProperty]
    private Key _selectedHotkeyKey;

    [ObservableProperty]
    private ThemeModeOption? _selectedTheme;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private string _startupStatusText = string.Empty;

    [ObservableProperty]
    private string _hotkeyStatusText = string.Empty;

    partial void OnHotkeyEnabledChanged(bool value)
    {
        PushHotkeyChanges();
    }

    partial void OnSelectedHotkeyModifierChanged(HotkeyModifierOption? value)
    {
        PushHotkeyChanges();
    }

    partial void OnSelectedHotkeyKeyChanged(Key value)
    {
        PushHotkeyChanges();
    }

    partial void OnSelectedThemeChanged(ThemeModeOption? value)
    {
        if (_isUpdating || value is null)
        {
            return;
        }

        Controller.UpdateTheme(value.Mode);
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_isUpdating)
        {
            return;
        }

        Controller.UpdateStartupSetting(value);
        UpdateStartupStatusText(Controller.Settings.StartWithWindows);
    }

    protected override void RefreshFromState()
    {
        _isUpdating = true;

        var settings = Controller.Settings;

        HotkeyEnabled = settings.Hotkey.Enabled;
        SelectedHotkeyModifier = HotkeyModifierOptions.FirstOrDefault(x => x.Modifiers == settings.Hotkey.Modifiers)
            ?? HotkeyModifierOptions.FirstOrDefault();
        SelectedHotkeyKey = KeyInterop.KeyFromVirtualKey(settings.Hotkey.VirtualKey);
        SelectedTheme = ThemeOptions.FirstOrDefault(x => x.Mode == settings.ThemeMode)
            ?? ThemeOptions.FirstOrDefault();
        StartWithWindows = settings.StartWithWindows;
        UpdateStartupStatusText(settings.StartWithWindows);
        HotkeyStatusText = Controller.HotkeyStatusText;

        _isUpdating = false;
    }

    private void UpdateStartupStatusText(bool enabled)
    {
        StartupStatusText = enabled
            ? "当前状态：已开启。Windows 登录后软件会静默驻留托盘，不会主动弹出主窗口，可从托盘图标恢复面板。"
            : "当前状态：未开启。软件仅在你手动打开时启动，并直接显示主窗口。";
    }

    private void PushHotkeyChanges()
    {
        if (_isUpdating || SelectedHotkeyModifier is null || SelectedHotkeyKey == Key.None)
        {
            return;
        }

        Controller.UpdateHotkeySettings(HotkeyEnabled, SelectedHotkeyModifier.Modifiers, SelectedHotkeyKey);
    }
}
