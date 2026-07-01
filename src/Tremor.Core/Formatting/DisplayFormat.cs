using System.Globalization;

namespace Tremor.Core.Formatting;

/// <summary>
/// Shared, culture-invariant formatting for market values so the same rules apply
/// everywhere (watchlist rows, detail screen, notifications) and can be tested.
/// </summary>
public static class DisplayFormat
{
    /// <summary>
    /// Compact large numbers: 1_234 → "1.23K", 2_500_000 → "2.5M", 1.1e9 → "1.1B".
    /// Small values keep up to two decimals. Negative values are prefixed with "-".
    /// </summary>
    public static string Compact(decimal value)
    {
        if (value < 0)
        {
            return "-" + Compact(-value);
        }

        return value switch
        {
            >= 1_000_000_000m => Trim(value / 1_000_000_000m) + "B",
            >= 1_000_000m => Trim(value / 1_000_000m) + "M",
            >= 1_000m => Trim(value / 1_000m) + "K",
            _ => Trim(value),
        };
    }

    /// <summary>
    /// Price with a sensible number of decimals: larger prices show fewer decimals,
    /// sub-dollar prices show more so small tokens stay readable.
    /// </summary>
    public static string Price(decimal value)
    {
        var abs = Math.Abs(value);
        var decimals = abs switch
        {
            >= 1_000m => 2,
            >= 1m => 4,
            >= 0.01m => 6,
            _ => 8,
        };

        return value.ToString("N" + decimals, CultureInfo.InvariantCulture);
    }

    /// <summary>Signed percentage, e.g. 1.2 → "+1.20%", -0.5 → "-0.50%".</summary>
    public static string SignedPercent(decimal value)
        => value.ToString("+0.00;-0.00", CultureInfo.InvariantCulture) + "%";

    private static string Trim(decimal value)
    {
        // Two decimals, then drop trailing zeros: 2.50 → "2.5", 3.00 → "3".
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return rounded.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
