using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Affiliate;

/// <summary>
/// Builds outbound exchange deep links from the configured template. These links
/// route the user OUT to an exchange (carrying a referral code). Tremor never
/// executes a trade in-app and never holds funds — this only produces a URL.
/// </summary>
public sealed class AffiliateLinkService : IAffiliateLinkService
{
    private const string SymbolPlaceholder = "{symbol}";

    private readonly AffiliateOptions _options;

    public AffiliateLinkService(IOptions<TremorOptions> options)
    {
        _options = options.Value.Affiliate;
    }

    public Uri BuildExchangeLink(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var encoded = Uri.EscapeDataString(symbol.ToUpperInvariant());
        var url = _options.ExchangeLinkTemplate.Replace(
            SymbolPlaceholder,
            encoded,
            StringComparison.OrdinalIgnoreCase);

        return new Uri(url, UriKind.Absolute);
    }
}
