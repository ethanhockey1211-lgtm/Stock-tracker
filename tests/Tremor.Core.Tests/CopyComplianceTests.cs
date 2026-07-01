using System.Reflection;
using Tremor.Core.Copy;
using Xunit;

namespace Tremor.Core.Tests;

/// <summary>
/// Enforces the project's non-negotiable copy rules against every user-facing
/// string constant in <see cref="AppCopy"/>. If someone adds copy that reads like
/// a prediction or a buy/sell recommendation, these tests fail before it ships.
/// </summary>
public class CopyComplianceTests
{
    public static IEnumerable<object[]> AppCopyStrings()
    {
        foreach (var field in typeof(AppCopy).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field is { IsLiteral: true, FieldType: var t } && t == typeof(string))
            {
                yield return [field.Name, (string)field.GetRawConstantValue()!];
            }
        }
    }

    [Theory]
    [MemberData(nameof(AppCopyStrings))]
    public void AppCopyStringsAreCompliant(string name, string value)
    {
        var violations = CopyGuard.FindViolations(value);
        Assert.True(violations.Count == 0, $"{name} violates copy rules: {string.Join("; ", violations)}");
    }

    [Theory]
    [InlineData("BTC will pump next week")]
    [InlineData("This is about to moon")]
    [InlineData("Strong buy signal detected")]
    [InlineData("Time to sell before the drop")]
    [InlineData("Our forecast: 10x")]
    public void GuardCatchesForbiddenLanguage(string bad)
    {
        Assert.False(CopyGuard.IsCompliant(bad));
    }

    [Theory]
    [InlineData("Volume spike detected on BTCUSDT")]
    [InlineData("Large wallet movement detected")]
    [InlineData("SOL/USDT is now trading on Binance")]
    public void GuardAllowsFactualLanguage(string good)
    {
        Assert.True(CopyGuard.IsCompliant(good));
    }
}
