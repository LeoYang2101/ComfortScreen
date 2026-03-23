using System.Windows.Controls;
using ComfortScreen.ViewModels;

namespace ComfortScreen.Views.Pages;

public partial class EyeCarePage : Page
{
    public EyeCarePage()
    {
        InitializeComponent();
        DataContext = App.GetService<EyeCareViewModel>();
    }
}
