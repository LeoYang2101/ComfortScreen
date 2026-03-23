using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ComfortScreen.Infrastructure;
using ComfortScreen.Models;
using Wpf.Ui.Controls;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;

namespace ComfortScreen.Views;

public sealed class ThemedMessageWindow : Window
{
    private readonly WpfMessageBoxButton _buttons;

    public ThemedMessageWindow(string title, string message, WpfMessageBoxButton buttons, AppDialogKind kind)
    {
        _buttons = buttons;

        Title = title;
        Width = 460;
        MinHeight = 220;
        SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        AllowsTransparency = true;
        Background = WpfBrushes.Transparent;
        Icon = AppIconProvider.GetWindowIconSource();

        PreviewKeyDown += OnPreviewKeyDown;

        var root = new Border
        {
            CornerRadius = new CornerRadius(20),
            BorderThickness = new Thickness(1),
            Background = GetBrush("ComfortScreenDialogBackgroundBrush", MediaColor.FromRgb(250, 252, 255)),
            BorderBrush = GetBrush("ComfortScreenDialogTitleBorderBrush", MediaColor.FromArgb(30, 255, 255, 255))
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBar = BuildTitleBar(title);
        layout.Children.Add(titleBar);

        var contentPanel = BuildContent(message, kind);
        Grid.SetRow(contentPanel, 1);
        layout.Children.Add(contentPanel);

        var footer = BuildFooter(buttons, kind);
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        root.Child = layout;
        Content = root;
    }

    public WpfMessageBoxResult Result { get; private set; } = WpfMessageBoxResult.None;

    private UIElement BuildTitleBar(string title)
    {
        var border = new Border
        {
            Background = GetBrush("ComfortScreenDialogTitleBackgroundBrush", MediaColor.FromRgb(238, 244, 252)),
            BorderBrush = GetBrush("ComfortScreenDialogTitleBorderBrush", MediaColor.FromArgb(18, 255, 255, 255)),
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
            new WpfTextBlock
            {
                Text = title,
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
        closeButton.Click += (_, _) => CloseWithDefaultResult();
        closeButton.MouseEnter += (_, _) => closeButton.Background = GetBrush("ComfortScreenDialogCloseButtonHoverBrush", MediaColor.FromArgb(30, 255, 255, 255));
        closeButton.MouseLeave += (_, _) => closeButton.Background = WpfBrushes.Transparent;
        Grid.SetColumn(closeButton, 1);
        grid.Children.Add(closeButton);

        border.Child = grid;
        return border;
    }

    private UIElement BuildContent(string message, AppDialogKind kind)
    {
        var grid = new Grid
        {
            Margin = new Thickness(22, 20, 22, 18)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconBadge = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(21),
            Background = GetBrush("ComfortScreenDialogIconBackgroundBrush", MediaColor.FromRgb(229, 239, 255)),
            VerticalAlignment = VerticalAlignment.Top
        };
        iconBadge.Child = new WpfTextBlock
        {
            Text = GetKindGlyph(kind),
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = GetBrush("ComfortScreenDialogIconForegroundBrush", MediaColor.FromRgb(36, 123, 232))
        };
        grid.Children.Add(iconBadge);

        var messageBlock = new WpfTextBlock
        {
            Margin = new Thickness(16, 2, 0, 0),
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 24,
            FontSize = 14,
            Foreground = GetBrush("ComfortScreenDialogPrimaryTextBrush", MediaColor.FromRgb(30, 41, 59))
        };
        Grid.SetColumn(messageBlock, 1);
        grid.Children.Add(messageBlock);

        return grid;
    }

    private UIElement BuildFooter(WpfMessageBoxButton buttons, AppDialogKind kind)
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

        if (buttons == WpfMessageBoxButton.OKCancel || buttons == WpfMessageBoxButton.YesNoCancel)
        {
            panel.Children.Add(CreateActionButton("取消", ControlAppearance.Secondary, () => CloseWithResult(WpfMessageBoxResult.Cancel), false, true));
        }

        if (buttons == WpfMessageBoxButton.YesNo || buttons == WpfMessageBoxButton.YesNoCancel)
        {
            panel.Children.Add(CreateActionButton("否", ControlAppearance.Secondary, () => CloseWithResult(WpfMessageBoxResult.No), false, false));
            panel.Children.Add(CreateActionButton("是", kind == AppDialogKind.Error ? ControlAppearance.Caution : ControlAppearance.Primary, () => CloseWithResult(WpfMessageBoxResult.Yes), true, false));
            footer.Child = panel;
            return footer;
        }

        var primaryLabel = "确定";
        panel.Children.Add(CreateActionButton(primaryLabel, kind == AppDialogKind.Error ? ControlAppearance.Caution : ControlAppearance.Primary, () => CloseWithResult(WpfMessageBoxResult.OK), true, false));

        footer.Child = panel;
        return footer;
    }

    private Wpf.Ui.Controls.Button CreateActionButton(
        string content,
        ControlAppearance appearance,
        Action onClick,
        bool isDefault,
        bool isCancel
    )
    {
        var button = new Wpf.Ui.Controls.Button
        {
            Content = content,
            Appearance = appearance,
            MinWidth = 96,
            Margin = new Thickness(10, 0, 0, 0),
            IsDefault = isDefault,
            IsCancel = isCancel
        };

        button.Click += (_, _) => onClick();
        return button;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseWithDefaultResult();
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CloseWithResult(WpfMessageBoxResult.OK);
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

    private void CloseWithDefaultResult()
    {
        var fallback = _buttons switch
        {
            WpfMessageBoxButton.OK => WpfMessageBoxResult.OK,
            WpfMessageBoxButton.YesNo => WpfMessageBoxResult.No,
            _ => WpfMessageBoxResult.Cancel
        };
        CloseWithResult(fallback);
    }

    private void CloseWithResult(WpfMessageBoxResult result)
    {
        Result = result;
        Close();
    }

    private static string GetKindGlyph(AppDialogKind kind)
    {
        return kind switch
        {
            AppDialogKind.Error => "!",
            AppDialogKind.Warning => "!",
            AppDialogKind.Confirm => "?",
            _ => "i"
        };
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
