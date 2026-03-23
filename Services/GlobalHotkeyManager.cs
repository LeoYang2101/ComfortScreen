using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ComfortScreen.Services;

public sealed class GlobalHotkeyManager : IDisposable
{
    private readonly Window _window;
    private readonly int _id;
    private HwndSource? _source;
    private IntPtr _handle;

    public event EventHandler? Pressed;

    public GlobalHotkeyManager(Window window, int id = 9000)
    {
        _window = window;
        _id = id;
    }

    public bool Register(int modifiers, int virtualKey)
    {
        Unregister();

        _handle = new WindowInteropHelper(_window).EnsureHandle();
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);

        return RegisterHotKey(_handle, _id, modifiers, virtualKey);
    }

    public void Unregister()
    {
        if (_handle != IntPtr.Zero)
        {
            UnregisterHotKey(_handle, _id);
        }

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }

        _handle = IntPtr.Zero;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmHotKey = 0x0312;
        if (msg == WmHotKey && wParam.ToInt32() == _id)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
