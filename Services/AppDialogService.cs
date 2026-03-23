using System.Windows;
using ComfortScreen.Contracts;
using ComfortScreen.Models;
using ComfortScreen.Views;

namespace ComfortScreen.Services;

public sealed class AppDialogService : IAppDialogService
{
    public void ShowInfo(string title, string message, Window? owner = null)
    {
        Show(title, message, MessageBoxButton.OK, AppDialogKind.Information, owner);
    }

    public void ShowError(string title, string message, Window? owner = null)
    {
        Show(title, message, MessageBoxButton.OK, AppDialogKind.Error, owner);
    }

    public MessageBoxResult Show(string title, string message, MessageBoxButton buttons, AppDialogKind kind, Window? owner = null)
    {
        if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            return System.Windows.Application.Current.Dispatcher.Invoke(() => Show(title, message, buttons, kind, owner));
        }

        owner ??= System.Windows.Application.Current.MainWindow is { IsLoaded: true, IsVisible: true } mainWindow
            ? mainWindow
            : null;

        var window = new ThemedMessageWindow(title, message, buttons, kind);

        if (owner is not null && owner.IsLoaded && owner.IsVisible)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.ShowDialog();
        return window.Result;
    }
}
