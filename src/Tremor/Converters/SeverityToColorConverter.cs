using System.Globalization;
using Tremor.Core.Models;

namespace Tremor.Converters;

/// <summary>Maps an <see cref="AlertSeverity"/> to an accent color.</summary>
public sealed class SeverityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is AlertSeverity severity
            ? severity switch
            {
                AlertSeverity.High => Color.FromArgb("#F6465D"),
                AlertSeverity.Notable => Color.FromArgb("#F0B90B"),
                _ => Color.FromArgb("#848E9C"),
            }
            : Color.FromArgb("#848E9C");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
