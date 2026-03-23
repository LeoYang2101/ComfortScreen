using System.Windows.Controls;
using ComfortScreen.ViewModels;

namespace ComfortScreen.Views.Pages;

public partial class ReminderPage : Page
{
    public ReminderPage()
    {
        InitializeComponent();
        DataContext = App.GetService<ReminderViewModel>();
    }
}
