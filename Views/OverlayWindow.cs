using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ComfortScreen.Infrastructure;

namespace ComfortScreen.Views;

public sealed class OverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExLayered = 0x80000;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private static readonly IntPtr HwndTopmost = new(-1);

    public OverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        IsHitTestVisible = false;
        ShowActivated = false;
        Focusable = false;
        Icon = AppIconProvider.GetWindowIconSource();

        SourceInitialized += (_, _) => EnableClickThrough();
    }

    public void UpdateBounds(double left, double top, double width, double height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public void Apply(System.Windows.Media.Color color, double opacity)
    {
        Background = new System.Windows.Media.SolidColorBrush(color);
        Opacity = Math.Clamp(opacity, 0, 0.95);
    }

    internal void EnsureVisibleAndTopmost()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState != WindowState.Normal)
        {
            WindowState = WindowState.Normal;
        }

        Topmost = true;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        EnableClickThrough();
        SetWindowPos(
            handle,
            HwndTopmost,
            (int)Math.Round(Left),
            (int)Math.Round(Top),
            Math.Max(1, (int)Math.Round(Width)),
            Math.Max(1, (int)Math.Round(Height)),
            SwpNoActivate | SwpShowWindow | SwpNoOwnerZOrder
        );
    }

    private void EnableClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var exStyle = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        exStyle |= WsExLayered | WsExTransparent | WsExToolWindow | WsExNoActivate;
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(exStyle));
    }

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags
    );
}
