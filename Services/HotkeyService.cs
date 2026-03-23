using System.Windows;
using ComfortScreen.Contracts;
using ComfortScreen.Models;

namespace ComfortScreen.Services;

public sealed class HotkeyService : IHotkeyService
{
    private GlobalHotkeyManager? _hotkeyManager;
    private EventHandler? _registeredHandler;

    public void Initialize(Window window)
    {
        _hotkeyManager ??= new GlobalHotkeyManager(window);
    }

    public bool Register(HotkeySettings settings, EventHandler handler)
    {
        if (_hotkeyManager is null)
        {
            return false;
        }

        Unregister();
        _registeredHandler = handler;
        _hotkeyManager.Pressed += handler;

        if (!settings.Enabled)
        {
            return false;
        }

        return _hotkeyManager.Register(settings.Modifiers, settings.VirtualKey);
    }

    public void Unregister()
    {
        if (_hotkeyManager is null)
        {
            return;
        }

        if (_registeredHandler is not null)
        {
            _hotkeyManager.Pressed -= _registeredHandler;
            _registeredHandler = null;
        }

        _hotkeyManager.Unregister();
    }

    public void Dispose()
    {
        Unregister();
        _hotkeyManager?.Dispose();
        _hotkeyManager = null;
    }
}
