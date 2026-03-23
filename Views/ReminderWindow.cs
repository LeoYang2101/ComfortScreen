using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ComfortScreen.Infrastructure;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using WpfApplication = System.Windows.Application;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;
using WpfOrientation = System.Windows.Controls.Orientation;
using UiButton = Wpf.Ui.Controls.Button;
using UiControlAppearance = Wpf.Ui.Controls.ControlAppearance;

namespace ComfortScreen.Views;

public sealed class ReminderWindow : Window
{
    private TextBlock _countdownText = null!;
    private readonly DispatcherTimer _timer;
    private int _remainingSeconds;

    public ReminderWindow(int holdSeconds)
    {
        _remainingSeconds = Math.Max(5, holdSeconds);

        Title = "20-20-20 护眼提醒";
        Width = 460;
        MinHeight = 250;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = WpfBrushes.Transparent;
        Icon = AppIconProvider.GetWindowIconSource();
        PreviewKeyDown += OnPreviewKeyDown;

        var root = new Border
        {
            CornerRadius = new CornerRadius(20),
            BorderThickness = new Thickness(1),
            Background = GetBrush("ComfortScreenDialogBackgroundBrush", MediaColor.FromRgb(250, 252, 255)),
            BorderBrush = GetBrush("ComfortScreenDialogTitleBorderBrush", MediaColor.FromRgb(215, 225, 237))
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        layout.Children.Add(BuildTitleBar());

        var content = BuildContent();
        Grid.SetRow(content, 1);
        layout.Children.Add(content);

        var footer = BuildFooter();
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        root.Child = layout;
        Content = root;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += (_, _) => Tick();
        Loaded += (_, _) =>
        {
            UpdateText();
            _timer.Start();
        };
        Closed += (_, _) => _timer.Stop();
    }

    private UIElement BuildTitleBar()
    {
        var border = new Border
        {
            Background = GetBrush("ComfortScreenDialogTitleBackgroundBrush", MediaColor.FromRgb(238, 244, 252)),
            BorderBrush = GetBrush("ComfortScreenDialogTitleBorderBrush", MediaColor.FromRgb(215, 225, 237)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(20, 20, 0, 0),
            Padding = new Thickness(18, 14, 14, 14)
        };
        border.MouseLeftButtonDown += (_, _) => DragMoveSafely();

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titlePanel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        titlePanel.Children.Add(
            new System.Windows.Controls.Image
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 10, 0),
                Source = AppIconProvider.GetTitleBarIconSource()
            }
        );
        titlePanel.Children.Add(
            new TextBlock
            {
                Text = Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = GetBrush("ComfortScreenDialogTitleTextBrush", MediaColor.FromRgb(30, 41, 59))
            }
        );
        grid.Children.Add(titlePanel);

        var closeButton = new System.Windows.Controls.Button
        {
            Content = "×",
            Width = 30,
            Height = 30,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Cursor = WpfCursors.Hand,
            Background = WpfBrushes.Transparent,
            BorderBrush = WpfBrushes.Transparent,
            Foreground = GetBrush("ComfortScreenDialogTitleTextBrush", MediaColor.FromRgb(30, 41, 59))
        };
        closeButton.Click += (_, _) => Close();
        closeButton.MouseEnter += (_, _) => closeButton.Background = GetBrush("ComfortScreenDialogCloseButtonHoverBrush", MediaColor.FromRgb(225, 233, 243));
        closeButton.MouseLeave += (_, _) => closeButton.Background = WpfBrushes.Transparent;
        Grid.SetColumn(closeButton, 1);
        grid.Children.Add(closeButton);

        border.Child = grid;
        return border;
    }

    private UIElement BuildContent()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(24, 22, 24, 18)
        };

        var iconRow = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal
        };

        var iconBadge = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(21),
            Background = GetBrush("ComfortScreenDialogIconBackgroundBrush", MediaColor.FromRgb(229, 239, 255)),
            VerticalAlignment = VerticalAlignment.Top
        };
        iconBadge.Child = new TextBlock
        {
            Text = "i",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = GetBrush("ComfortScreenDialogIconForegroundBrush", MediaColor.FromRgb(36, 123, 232))
        };
        iconRow.Children.Add(iconBadge);

        var textStack = new StackPanel
        {
            Margin = new Thickness(14, 0, 0, 0)
        };
        textStack.Children.Add(
            new TextBlock
            {
                Text = "请看向 20 英尺（约 6 米）以外，放松眼睛。",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetBrush("ComfortScreenDialogPrimaryTextBrush", MediaColor.FromRgb(30, 41, 59))
            }
        );
        textStack.Children.Add(
            new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Text = "短暂远眺有助于缓解眼疲劳，也能让注意力更稳定。",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetBrush("ComfortScreenDialogSecondaryTextBrush", MediaColor.FromRgb(82, 98, 121))
            }
        );

        iconRow.Children.Add(textStack);
        panel.Children.Add(iconRow);

        _countdownText = new TextBlock
        {
            Margin = new Thickness(0, 20, 0, 0),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush("ComfortScreenDialogAccentBrush", MediaColor.FromRgb(36, 123, 232))
        };
        panel.Children.Add(_countdownText);

        return panel;
    }

    private UIElement BuildFooter()
    {
        var footer = new Border
        {
            Background = GetBrush("ComfortScreenDialogFooterBackgroundBrush", MediaColor.FromRgb(244, 248, 252)),
            BorderBrush = GetBrush("ComfortScreenDialogTitleBorderBrush", MediaColor.FromRgb(215, 225, 237)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, 20, 20),
            Padding = new Thickness(18, 14, 18, 18)
        };

        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var button = new UiButton
        {
            Content = "我已完成休息",
            Appearance = UiControlAppearance.Primary,
            MinWidth = 124,
            IsDefault = true,
            IsCancel = true
        };
        button.Click += (_, _) => Close();

        panel.Children.Add(button);
        footer.Child = panel;
        return footer;
    }

    private void Tick()
    {
        _remainingSeconds--;
        UpdateText();

        if (_remainingSeconds <= 0)
        {
            Close();
        }
    }

    private void UpdateText()
    {
        _countdownText.Text = $"保持远眺 {Math.Max(_remainingSeconds, 0)} 秒";
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.Enter)
        {
            e.Handled = true;
            Close();
        }
    }

    private void DragMoveSafely()
    {
        try
        {
            DragMove();
        }
        catch
        {
            // Ignore drag failures when pointer state changes unexpectedly.
        }
    }

    private static MediaBrush GetBrush(string key, MediaColor fallbackColor)
    {
        if (WpfApplication.Current.Resources[key] is MediaBrush brush)
        {
            return brush;
        }

        var fallback = new SolidColorBrush(fallbackColor);
        fallback.Freeze();
        return fallback;
    }
}
