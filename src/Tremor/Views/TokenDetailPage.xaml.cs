using Tremor.ViewModels;

namespace Tremor.Views;

public partial class TokenDetailPage : ContentPage
{
    public TokenDetailPage(TokenDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
