using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ComfortScreen.Infrastructure;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;

namespace ComfortScreen.Views;

public sealed class BreakOverlayWindow : Window
{
    private readonly TextBlock _countdown;

    public event EventHandler? ExitRequested;

    public BreakOverlayWindow(bool allowManualExit)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = new SolidColorBrush(MediaColor.FromArgb(225, 16, 24, 40));
        Icon = AppIconProvider.GetWindowIconSource();

        var root = new Grid
        {
            Margin = new Thickness(24)
        };

        var panel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };

        var title = new TextBlock
        {
            Text = "休息时间",
            Foreground = GetBrush("ComfortScreenOverlayPrimaryTextBrush", MediaColor.FromRgb(255, 255, 255)),
            FontSize = 40,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };

        _countdown = new TextBlock
        {
            Margin = new Thickness(0, 16, 0, 0),
            Foreground = GetBrush("ComfortScreenOverlayAccentBrush", MediaColor.FromRgb(164, 198, 255)),
            FontSize = 28,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };

        panel.Children.Add(title);
        panel.Children.Add(_countdown);

        if (allowManualExit)
        {
            var exitButton = new System.Windows.Controls.Button
            {
                Content = "退出休息遮罩",
                Width = 140,
                Margin = new Thickness(0, 20, 0, 0),
                Padding = new Thickness(10, 6, 10, 6),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Background = GetBrush("ComfortScreenOverlayButtonBackgroundBrush", MediaColor.FromRgb(229, 239, 255)),
                Foreground = GetBrush("ComfortScreenOverlayButtonForegroundBrush", MediaColor.FromRgb(31, 41, 55))
            };
            exitButton.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
            panel.Children.Add(exitButton);
        }

        root.Children.Add(panel);
        Content = root;
    }

    public void UpdateCountdown(TimeSpan remaining)
    {
        _countdown.Text = $"请离开屏幕，剩余 {remaining:mm\\:ss}";
    }

    private static MediaBrush GetBrush(string key, MediaColor fallbackColor)
    {
        if (System.Windows.Application.Current.Resources[key] is MediaBrush brush)
        {
            return brush;
        }

        var fallback = new SolidColorBrush(fallbackColor);
        fallback.Freeze();
        return fallback;
    }
}

