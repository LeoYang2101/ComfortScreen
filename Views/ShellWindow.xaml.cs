using System.ComponentModel;
using System.Linq;
using System.Windows;
using ComfortScreen.Services;
using ComfortScreen.ViewModels;
using ComfortScreen.Views.Pages;
using Wpf.Ui.Controls;

namespace ComfortScreen.Views;

public partial class ShellWindow : FluentWindow
{
    private readonly ComfortScreenController _controller;
    private readonly ShellViewModel _viewModel;
    private bool _allowRealClose;
    private bool _initialNavigationCompleted;

    public ShellWindow(ComfortScreenController controller, ShellViewModel viewModel)
    {
        _controller = controller;
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closing += OnClosing;

        _viewModel.MinimizeRequested += (_, _) => Hide();
        _controller.RestoreRequested += (_, _) => RestoreFromTray();
        _controller.ExitRequested += (_, _) => ExitApplication();
    }

    public void InitializeShell()
    {
        _controller.Initialize(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CompleteInitialNavigation();
    }

    private void CompleteInitialNavigation()
    {
        if (!_initialNavigationCompleted)
        {
            _initialNavigationCompleted = true;
            RootNavigation.Navigate(typeof(DashboardPage));
        }

        Loaded -= OnLoaded;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowRealClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _controller.Shutdown();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _allowRealClose = true;
        Close();
    }
}
