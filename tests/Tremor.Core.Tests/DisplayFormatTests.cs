using Tremor.Core.Formatting;
using Xunit;

namespace Tremor.Core.Tests;

public class DisplayFormatTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1000, "1K")]
    [InlineData(1234, "1.23K")]
    [InlineData(2_500_000, "2.5M")]
    [InlineData(1_100_000_000, "1.1B")]
    [InlineData(3_000_000, "3M")]
    public void CompactFormatsMagnitudes(decimal value, string expected)
    {
        Assert.Equal(expected, DisplayFormat.Compact(value));
    }

    [Fact]
    public void CompactHandlesNegatives()
    {
        Assert.Equal("-2.5M", DisplayFormat.Compact(-2_500_000m));
    }

    [Theory]
    [InlineData(1234.5678, "1,234.57")]
    [InlineData(12.3456, "12.3456")]
    [InlineData(0.123456, "0.123456")]
    [InlineData(0.00001234, "0.00001234")]
    public void PriceScalesDecimalsByMagnitude(decimal value, string expected)
    {
        Assert.Equal(expected, DisplayFormat.Price(value));
    }

    [Theory]
    [InlineData(1.2, "+1.20%")]
    [InlineData(-0.5, "-0.50%")]
    [InlineData(0, "+0.00%")]
    public void SignedPercentIncludesSign(decimal value, string expected)
    {
        Assert.Equal(expected, DisplayFormat.SignedPercent(value));
    }
}
