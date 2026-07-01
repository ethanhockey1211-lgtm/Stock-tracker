using Tremor.Views;

namespace Tremor;

public partial class AppShell : Shell
{
    public const string TokenDetailRoute = "tokendetail";

    public AppShell()
    {
        InitializeComponent();

        // Detail page is reached by navigation, not a tab.
        Routing.RegisterRoute(TokenDetailRoute, typeof(TokenDetailPage));
    }
}
