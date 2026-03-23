using System.Windows;
using ComfortScreen.Models;

namespace ComfortScreen.Contracts;

public interface IAppDialogService
{
    void ShowInfo(string title, string message, Window? owner = null);

    void ShowError(string title, string message, Window? owner = null);

    MessageBoxResult Show(string title, string message, MessageBoxButton buttons, AppDialogKind kind, Window? owner = null);
}
