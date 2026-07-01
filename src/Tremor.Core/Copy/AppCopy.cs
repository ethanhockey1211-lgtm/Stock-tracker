namespace Tremor.Core.Copy;

/// <summary>
/// Centralized, review-controlled user-facing strings.
///
/// HARD RULES (non-negotiable — see project brief). All copy here must obey them,
/// and <see cref="CopyGuard"/> is unit-tested against this class:
///   1. Never claim to predict the future. Frame everything as "detected" or
///      "happening now" — never "will", "about to", "forecast", or "prediction".
///   2. No buy/sell recommendations anywhere. Alerts show data; the user decides.
///   3. The app never holds funds or trades. "Trade this" links route OUT to an
///      exchange; they never execute in-app.
///
/// Keeping copy in one place makes rule 7 ("flag anything that reads like a
/// prediction or recommendation instead of shipping it") enforceable in CI.
/// </summary>
public static class AppCopy
{
    public const string AppName = "Tremor";
    public const string Tagline = "See what's happening in crypto markets — as it happens.";

    // Watchlist
    public const string WatchlistTitle = "Watchlist";
    public const string WatchlistEmpty = "Add a token to start tracking its live price and volume.";
    public const string AddToken = "Add token";

    // Alerts
    public const string AlertsTitle = "Alerts";
    public const string AlertsEmpty = "No events detected yet. We'll surface volume spikes, large wallet moves, and new listings here as they happen.";
    public const string VolumeSpikeHeadline = "Volume spike detected";
    public const string WhaleMovementHeadline = "Large wallet movement detected";
    public const string NewListingHeadline = "New listing detected";

    // Listings
    public const string ListingsTitle = "New listings";
    public const string ListingsEmpty = "No new listings detected recently.";

    // Trade-out / affiliate (routes to an exchange; never executes in-app)
    public const string ViewOnExchange = "View on exchange";
    public const string ViewOnExchangeNote = "Opens your exchange in a browser. Tremor never holds funds or places trades.";

    // Settings
    public const string SettingsTitle = "Settings";
    public const string PushNotificationsLabel = "Push notifications";
    public const string TierLabelFree = "Free";
    public const string TierLabelPremium = "Premium";

    // Compliance footer shown where alerts are displayed.
    public const string Disclaimer =
        "Tremor reports market data that has already been observed. It is not investment advice, "
        + "makes no predictions, and gives no buy or sell recommendations. You decide what to do with the information.";
}
