using Tremor.Core.Services.Market;
using Xunit;

namespace Tremor.Core.Tests;

public class SymbolParserTests
{
    [Theory]
    [InlineData("BTCUSDT", "BTC", "USDT")]
    [InlineData("ethusdt", "ETH", "USDT")]
    [InlineData("BTC/USDT", "BTC", "USDT")]
    [InlineData("btc-usdt", "BTC", "USDT")]
    [InlineData("SOLUSDC", "SOL", "USDC")]
    [InlineData("ETHBTC", "ETH", "BTC")]
    public void ParsesKnownForms(string input, string expectedBase, string expectedQuote)
    {
        Assert.True(SymbolParser.TryParse(input, out var token));
        Assert.Equal(expectedBase, token.BaseAsset);
        Assert.Equal(expectedQuote, token.QuoteAsset);
        Assert.Equal(expectedBase + expectedQuote, token.Symbol);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("X")]
    [InlineData("HELLO")]
    public void RejectsInvalid(string input)
    {
        Assert.False(SymbolParser.TryParse(input, out _));
    }

    [Fact]
    public void PrefersLongerQuoteSuffix()
    {
        // "USDT" must win over "USD" so the base is BTC, not BTCT.
        Assert.True(SymbolParser.TryParse("BTCUSDT", out var token));
        Assert.Equal("USDT", token.QuoteAsset);
        Assert.Equal("BTC", token.BaseAsset);
    }
}
