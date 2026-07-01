using System.Globalization;
using Tremor.Core.Formatting;

namespace Tremor.Converters;

file static class DecimalCoercion
{
    public static decimal ToDecimal(object? value) => value switch
    {
        decimal d => d,
        double db => (decimal)db,
        float f => (decimal)f,
        long l => l,
        int i => i,
        _ => 0m,
    };
}

/// <summary>Formats a number compactly (e.g. 2.5M) via <see cref="DisplayFormat.Compact"/>.</summary>
public sealed class CompactNumberConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => DisplayFormat.Compact(DecimalCoercion.ToDecimal(value));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Formats a price with magnitude-aware decimals via <see cref="DisplayFormat.Price"/>.</summary>
public sealed class PriceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => DisplayFormat.Price(DecimalCoercion.ToDecimal(value));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Formats a signed percentage via <see cref="DisplayFormat.SignedPercent"/>.</summary>
public sealed class SignedPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => DisplayFormat.SignedPercent(DecimalCoercion.ToDecimal(value));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
