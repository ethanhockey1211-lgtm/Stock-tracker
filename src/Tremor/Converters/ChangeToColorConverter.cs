using System.Globalization;

namespace Tremor.Converters;

/// <summary>Green for a non-negative change, red for negative. Purely descriptive.</summary>
public sealed class ChangeToColorConverter : IValueConverter
{
    private static readonly Color Up = Color.FromArgb("#0ECB81");
    private static readonly Color Down = Color.FromArgb("#F6465D");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var change = value switch
        {
            decimal d => d,
            double db => (decimal)db,
            _ => 0m,
        };

        return change >= 0 ? Up : Down;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
