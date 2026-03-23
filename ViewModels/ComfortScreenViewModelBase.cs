using CommunityToolkit.Mvvm.ComponentModel;
using ComfortScreen.Services;

namespace ComfortScreen.ViewModels;

public abstract partial class ComfortScreenViewModelBase : ObservableObject
{
    protected ComfortScreenViewModelBase(ComfortScreenController controller)
    {
        Controller = controller;
        Controller.StateChanged += (_, _) => RefreshFromState();
    }

    protected ComfortScreenController Controller { get; }

    protected abstract void RefreshFromState();
}
