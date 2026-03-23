using System.Windows.Controls;
using ComfortScreen.ViewModels;

namespace ComfortScreen.Views.Pages;

public partial class DisplayPage : Page
{
    public DisplayPage()
    {
        InitializeComponent();
        DataContext = App.GetService<DisplayViewModel>();
    }
}
