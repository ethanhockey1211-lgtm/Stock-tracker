using CommunityToolkit.Mvvm.ComponentModel;

namespace Tremor.ViewModels;

/// <summary>Common base for view models: busy flag and a title.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;
}
