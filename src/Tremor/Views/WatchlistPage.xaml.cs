using Tremor.ViewModels;

namespace Tremor.Views;

public partial class WatchlistPage : ContentPage
{
    private readonly WatchlistViewModel _viewModel;

    public WatchlistPage(WatchlistViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.AppearingCommand.CanExecute(null))
        {
            _viewModel.AppearingCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.DisappearingCommand.Execute(null);
    }
}
