namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Builds outbound deep links to an exchange for a symbol. These links route the
/// user OUT to an exchange (with a referral code); Tremor never executes a trade
/// in-app and never holds funds.
/// </summary>
public interface IAffiliateLinkService
{
    /// <summary>Build the outbound exchange URL for a trading symbol.</summary>
    Uri BuildExchangeLink(string symbol);
}
