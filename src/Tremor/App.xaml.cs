using Tremor.Core.Services.Scanning;

namespace Tremor;

public partial class App : Application
{
    private readonly MarketScanner _scanner;
    private CancellationTokenSource? _scanCts;

    public App(MarketScanner scanner)
    {
        InitializeComponent();
        _scanner = scanner;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            Title = "Tremor",
        };

        window.Destroying += OnWindowDestroying;

        // Start the background market scanner for the lifetime of the window.
        _scanCts = new CancellationTokenSource();
        _ = _scanner.RunAsync(_scanCts.Token);

        return window;
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;
    }
}
