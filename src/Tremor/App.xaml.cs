using Microsoft.Extensions.Logging;
using Tremor.Core.Services.Scanning;

namespace Tremor;

public partial class App : Application
{
    private readonly MarketScanner _scanner;
    private readonly ILogger<App> _logger;
    private CancellationTokenSource? _scanCts;
    private Task? _scannerTask;

    public App(MarketScanner scanner, ILogger<App> logger)
    {
        InitializeComponent();
        _scanner = scanner;
        _logger = logger;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _logger.LogInformation("Creating main window");
        var window = new Window(new AppShell())
        {
            Title = "Tremor",
        };

        window.Destroying += OnWindowDestroying;

        // Start the background market scanner for the lifetime of the window.
        _scanCts = new CancellationTokenSource();
        _logger.LogInformation("Starting background market scanner");
        _scannerTask = _scanner.RunAsync(_scanCts.Token);
        _ = _scannerTask.ContinueWith(
            task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "Background market scanner stopped unexpectedly");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        return window;
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        _logger.LogInformation("Destroying main window; canceling scanner");
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;
    }
}
