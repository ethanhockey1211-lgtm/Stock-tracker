# Tremor

**See what's happening in crypto markets — as it happens.**

Tremor is a cross-platform (.NET MAUI) mobile app that scans crypto markets for
real signals that are *already happening* and surfaces them fast: volume spikes,
large wallet movements, and new listings. It is a **data tool**, not an adviser.

## Hard rules (non-negotiable)

These are enforced in code and copy, not just intent:

1. **No predictions.** Everything is framed as *detected* / *happening now* — never
   "will pump", "about to moon", or "forecast".
2. **No buy/sell recommendations.** Alerts show data; the user decides. There is no
   "buy signal" anywhere in the app.
3. **No custody, ever.** Tremor never holds funds and never executes trades. "View
   on exchange" buttons route *out* to an exchange via a referral link.

Copy is centralized in [`AppCopy`](src/Tremor.Core/Copy/AppCopy.cs) and linted by
[`CopyGuard`](src/Tremor.Core/Copy/CopyGuard.cs). `CopyComplianceTests` fails the
build if any user-facing string reads like a prediction or a recommendation.

## Phase 1 MVP scope (crypto only)

| Feature | Status | Where |
| --- | --- | --- |
| Live watchlist (price + volume) | ✅ | `WatchlistViewModel`, `BinanceMarketDataService` |
| Volume spike detection | ✅ | `VolumeSpikeDetector` (rolling baseline) |
| New listing feed | ✅ | `BinanceNewListingService` (exchangeInfo diff) |
| Whale wallet movement alerts | 🟡 placeholder | `PlaceholderWhaleAlertService` (interface + shaping ready; on-chain provider not wired) |
| Push notifications | 🟡 placeholder | `INotificationService` / `MauiLocalNotificationService` (logs; FCM/local-notification seam ready) |
| Free/Premium flag (not gated) | ✅ | `UserProfile.Tier` |
| Affiliate deep-link placeholder | ✅ | `AffiliateLinkService` (referral code is a placeholder) |

**Deferred to Phase 2+:** stock scanning, news aggregation, social sentiment,
options flow, AI pattern detection, real premium billing/paywall.

## Architecture

Two-project split so the important logic is testable without the MAUI workload:

```
Tremor.sln
├── src/
│   ├── Tremor.Core/     net9.0 class library — models, market-data clients,
│   │                    detection, service abstractions. No MAUI dependency.
│   └── Tremor/          net9.0 MAUI app head — Shell, pages, view models, DI wiring.
└── tests/
    └── Tremor.Core.Tests/  xUnit tests for detection, parsing, copy rules, etc.
```

- **Data source:** Binance **public** REST + combined WebSocket streams — no API
  key required (only public market data is read). Everything is read-only.
- **Detection runs on-device** in the MVP via `MarketScanner`, behind the same
  service interfaces a backend would implement — so the logic can move server-side
  later without touching the UI.
- **MVVM** with `CommunityToolkit.Mvvm`; DI via `Microsoft.Extensions.DependencyInjection`.

### Design decision (reversible)

The original brief suggested a React Native app + Node backend. Per the request,
this is built in **.NET MAUI**. For the MVP the client talks directly to Binance's
public endpoints; a backend is *not* required to prove the concept. The service
abstractions in `Tremor.Core` mean introducing a backend later is a config/DI swap,
not a rewrite.

## Building

Requires the .NET 9 SDK and the MAUI workload:

```bash
dotnet workload install maui

# Build the platform-agnostic core + run its tests (no MAUI workload needed):
dotnet build src/Tremor.Core/Tremor.Core.csproj
dotnet test  tests/Tremor.Core.Tests/Tremor.Core.Tests.csproj

# Build/run the app for a specific target:
dotnet build src/Tremor/Tremor.csproj -f net9.0-android
# iOS / MacCatalyst require a Mac with Xcode; Windows requires Windows.
```

> Note: this scaffold was authored in an environment without the .NET SDK, so it
> has not been compiled here. `Tremor.Core` + tests are structured to build and run
> with the plain .NET SDK; the MAUI head needs the MAUI workload.

## Configuration

Defaults work with zero configuration. Tunables live in
[`TremorOptions`](src/Tremor.Core/Configuration/TremorOptions.cs): Binance URLs,
poll interval, volume-spike thresholds, whale thresholds, and the affiliate link
template. **Replace `TREMOR_PLACEHOLDER`** in the affiliate template before shipping.

## Before shipping — checklist

- [ ] Wire a real on-chain provider (Etherscan/Moralis) into `IWhaleAlertService`.
- [ ] Implement real push (Firebase Cloud Messaging) + local notifications in
      `INotificationService`.
- [ ] Replace the affiliate referral placeholder with a real code.
- [ ] Add persistence for the watchlist and user profile (currently in-memory).
- [ ] Add app icons/splash bitmaps and (optionally) bundled fonts.
