namespace Tremor.Core.Models;

/// <summary>
/// Direction of a detected large on-chain movement, relative to exchanges.
/// </summary>
public enum WhaleFlowDirection
{
    /// <summary>Tokens moving into an exchange deposit address.</summary>
    ExchangeInflow,

    /// <summary>Tokens moving out of an exchange withdrawal address.</summary>
    ExchangeOutflow,

    /// <summary>A large wallet-to-wallet transfer not clearly tied to an exchange.</summary>
    WalletTransfer,
}

/// <summary>
/// A large, already-confirmed on-chain movement on a tracked asset.
/// Reports the transfer that happened; draws no conclusion about price.
/// </summary>
public sealed class WhaleAlert : Alert
{
    public WhaleAlert()
    {
        Type = AlertType.WhaleMovement;
    }

    public WhaleFlowDirection Direction { get; init; }

    /// <summary>Amount transferred, in base-asset units.</summary>
    public decimal Amount { get; init; }

    /// <summary>Approximate USD value of the transfer at detection time, if known.</summary>
    public decimal? ApproxUsdValue { get; init; }

    /// <summary>Source address (may be truncated for display).</summary>
    public string? FromAddress { get; init; }

    /// <summary>Destination address (may be truncated for display).</summary>
    public string? ToAddress { get; init; }

    /// <summary>On-chain transaction hash, when available.</summary>
    public string? TransactionHash { get; init; }
}
