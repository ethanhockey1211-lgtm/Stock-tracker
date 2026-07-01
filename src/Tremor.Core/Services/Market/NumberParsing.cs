using System.Globalization;

namespace Tremor.Core.Services.Market;

/// <summary>Helpers for parsing Binance's string-encoded numeric fields.</summary>
internal static class NumberParsing
{
    public static decimal ToDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
}
