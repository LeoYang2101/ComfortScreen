using ComfortScreen.Contracts;
using ComfortScreen.Views;
using System.Windows;
using WpfApplication = System.Windows.Application;

namespace ComfortScreen.Services;

public sealed class ReminderDialogService : IReminderDialogService
{
    public void Show(int holdSeconds)
    {
        if (!WpfApplication.Current.Dispatcher.CheckAccess())
        {
            WpfApplication.Current.Dispatcher.Invoke(() => Show(holdSeconds));
            return;
        }

        var window = new ReminderWindow(holdSeconds)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        if (WpfApplication.Current.MainWindow is { IsLoaded: true, IsVisible: true } owner)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        window.ShowDialog();
    }
}
