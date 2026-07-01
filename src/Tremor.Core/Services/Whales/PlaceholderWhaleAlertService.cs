using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Copy;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Whales;

/// <summary>
/// Placeholder whale-movement source for the MVP.
///
/// Real whale tracking requires an on-chain data provider (Etherscan / Moralis
/// free tier) to watch large transfers and known exchange deposit/withdrawal
/// addresses. That integration is intentionally not wired up yet — this class
/// implements the exact <see cref="IWhaleAlertService"/> contract the real
/// provider will satisfy, and returns an empty set so the rest of the app
/// (alerts feed, notifications, UI) can be built and tested end to end now.
///
/// TODO(phase-1): replace with an EtherscanWhaleAlertService that:
///   - queries ERC-20 transfer events above <see cref="WhaleOptions.MinUsdValue"/>,
///   - classifies against a known-exchange address list into
///     <see cref="WhaleFlowDirection"/>, and
///   - maps each qualifying transfer to a <see cref="WhaleAlert"/>.
/// </summary>
public sealed class PlaceholderWhaleAlertService : IWhaleAlertService
{
    private readonly WhaleOptions _options;
    private readonly ILogger<PlaceholderWhaleAlertService> _logger;

    public PlaceholderWhaleAlertService(
        IOptions<TremorOptions> options,
        ILogger<PlaceholderWhaleAlertService> logger)
    {
        _options = options.Value.Whale;
        _logger = logger;
    }

    public Task<IReadOnlyList<WhaleAlert>> GetRecentAsync(
        IEnumerable<string> baseAssets,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Whale detection is a placeholder in the MVP; returning no movements "
            + "(min size ${MinUsd}).",
            _options.MinUsdValue);

        return Task.FromResult<IReadOnlyList<WhaleAlert>>([]);
    }

    /// <summary>
    /// Maps a confirmed on-chain transfer into a rule-compliant alert. Exposed so
    /// the real provider (and tests) share one place that shapes whale alerts.
    /// </summary>
    public WhaleAlert CreateAlert(
        string baseAsset,
        WhaleFlowDirection direction,
        decimal amount,
        decimal approxUsdValue,
        string? txHash,
        DateTimeOffset detectedUtc)
    {
        var severity = approxUsdValue >= _options.HighSeverityUsdValue
            ? AlertSeverity.High
            : AlertSeverity.Notable;

        var directionText = direction switch
        {
            WhaleFlowDirection.ExchangeInflow => "moved into an exchange",
            WhaleFlowDirection.ExchangeOutflow => "moved out of an exchange",
            _ => "moved between wallets",
        };

        return new WhaleAlert
        {
            Id = $"whale:{baseAsset}:{txHash ?? detectedUtc.ToUnixTimeMilliseconds().ToString()}",
            Symbol = baseAsset,
            Severity = severity,
            Title = AppCopy.WhaleMovementHeadline,
            Detail = $"{amount:N0} {baseAsset} (~${approxUsdValue:N0}) {directionText}.",
            DetectedUtc = detectedUtc,
            Direction = direction,
            Amount = amount,
            ApproxUsdValue = approxUsdValue,
            TransactionHash = txHash,
        };
    }
}
