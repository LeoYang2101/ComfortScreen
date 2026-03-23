using System.Windows.Controls;
using ComfortScreen.ViewModels;

namespace ComfortScreen.Views.Pages;

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        DataContext = App.GetService<DashboardViewModel>();
    }
}
