using System.Windows;
using ComfortScreen.Models;

namespace ComfortScreen.Contracts;

public interface IHotkeyService : IDisposable
{
    void Initialize(Window window);

    bool Register(HotkeySettings settings, EventHandler handler);

    void Unregister();
}
