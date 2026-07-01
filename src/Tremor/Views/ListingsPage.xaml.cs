using Tremor.ViewModels;

namespace Tremor.Views;

public partial class ListingsPage : ContentPage
{
    private readonly ListingsViewModel _viewModel;

    public ListingsPage(ListingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Establish the listing baseline on first view; later refreshes surface
        // anything new detected since.
        _viewModel.RefreshCommand.Execute(null);
    }
}
