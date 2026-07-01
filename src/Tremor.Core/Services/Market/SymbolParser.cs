using Tremor.Core.Models;

namespace Tremor.Core.Services.Market;

/// <summary>
/// Best-effort parsing of user-entered symbols into a <see cref="Token"/>.
/// Accepts "BTCUSDT", "BTC/USDT", or "btc-usdt" and splits base/quote using a
/// list of common quote assets.
/// </summary>
public static class SymbolParser
{
    // Ordered longest-first so e.g. "USDT" matches before "USD".
    private static readonly string[] KnownQuotes =
    [
        "USDT", "USDC", "FDUSD", "TUSD", "BUSD", "USD",
        "BTC", "ETH", "BNB", "EUR", "TRY", "GBP",
    ];

    public static bool TryParse(string? input, out Token token)
    {
        token = default!;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Explicit separator form: BTC/USDT or BTC-USDT.
        var separators = new[] { '/', '-' };
        var raw = input.Trim().ToUpperInvariant();
        foreach (var sep in separators)
        {
            var idx = raw.IndexOf(sep);
            if (idx > 0 && idx < raw.Length - 1)
            {
                var b = OnlyAlphaNumeric(raw[..idx]);
                var q = OnlyAlphaNumeric(raw[(idx + 1)..]);
                if (b.Length > 0 && q.Length > 0)
                {
                    token = Build(b, q);
                    return true;
                }
            }
        }

        // Concatenated form: split on a known quote suffix.
        var symbol = OnlyAlphaNumeric(raw);
        if (symbol.Length < 2)
        {
            return false;
        }

        foreach (var quote in KnownQuotes)
        {
            if (symbol.Length > quote.Length && symbol.EndsWith(quote, StringComparison.Ordinal))
            {
                var baseAsset = symbol[..^quote.Length];
                token = Build(baseAsset, quote);
                return true;
            }
        }

        return false;
    }

    private static Token Build(string baseAsset, string quoteAsset) => new()
    {
        Symbol = baseAsset + quoteAsset,
        BaseAsset = baseAsset,
        QuoteAsset = quoteAsset,
    };

    private static string OnlyAlphaNumeric(string value)
        => new(value.Where(char.IsLetterOrDigit).ToArray());
}
