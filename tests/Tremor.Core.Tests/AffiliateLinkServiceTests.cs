using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Services.Affiliate;
using Xunit;

namespace Tremor.Core.Tests;

public class AffiliateLinkServiceTests
{
    private static AffiliateLinkService Create(string template)
    {
        var options = new TremorOptions { Affiliate = new AffiliateOptions { ExchangeLinkTemplate = template } };
        return new AffiliateLinkService(Options.Create(options));
    }

    [Fact]
    public void SubstitutesSymbolIntoTemplate()
    {
        var service = Create("https://exchange.example/register?ref=ABC&symbol={symbol}");
        var uri = service.BuildExchangeLink("btcusdt");

        Assert.Equal("https://exchange.example/register?ref=ABC&symbol=BTCUSDT", uri.ToString());
    }

    [Fact]
    public void ProducesAbsoluteUri()
    {
        var service = Create("https://exchange.example/{symbol}");
        var uri = service.BuildExchangeLink("ETHUSDT");
        Assert.True(uri.IsAbsoluteUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptySymbol(string symbol)
    {
        var service = Create("https://exchange.example/{symbol}");
        Assert.Throws<ArgumentException>(() => service.BuildExchangeLink(symbol));
    }
}
