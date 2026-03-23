using System.Windows.Controls;
using ComfortScreen.ViewModels;

namespace ComfortScreen.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.GetService<SettingsViewModel>();
    }
}
