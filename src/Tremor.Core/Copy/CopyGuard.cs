using System.Text.RegularExpressions;

namespace Tremor.Core.Copy;

/// <summary>
/// Guards user-facing copy against the project's hard rules. This is deliberately
/// conservative: it flags language that <em>reads like</em> a prediction or a
/// buy/sell recommendation so it can be caught in review/CI rather than shipped.
///
/// It is a lint, not a legal filter — it catches the obvious violations. Final
/// wording still gets human review.
/// </summary>
public static partial class CopyGuard
{
    // Phrases that imply forecasting the future. Note: the bare nouns
    // "prediction"/"predict" are deliberately NOT listed — copy that *disclaims*
    // predictions (e.g. "makes no predictions") is compliant. We flag concrete
    // forward-looking claims instead.
    private static readonly string[] PredictionPhrases =
    [
        "will pump", "about to pump", "about to moon", "going to moon",
        "will moon", "price target", "forecast", "guaranteed to",
        "will rise", "will fall", "will surge", "next 10x",
        "sure thing", "can't lose", "cannot lose",
    ];

    // Phrases that constitute investment advice / recommendations.
    private static readonly string[] RecommendationPhrases =
    [
        "buy now", "sell now", "you should buy", "you should sell",
        "we recommend buying", "we recommend selling", "strong buy",
        "strong sell", "buy signal", "sell signal", "time to buy", "time to sell",
    ];

    [GeneratedRegex(@"\bwill\s+(pump|moon|rise|fall|surge|crash|explode|10x|double)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FuturePredictionRegex();

    /// <summary>
    /// Returns the list of rule violations found in <paramref name="text"/>.
    /// An empty list means the copy passes the lint.
    /// </summary>
    public static IReadOnlyList<string> FindViolations(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var lower = text.ToLowerInvariant();
        var violations = new List<string>();

        foreach (var phrase in PredictionPhrases)
        {
            if (lower.Contains(phrase, StringComparison.Ordinal))
            {
                violations.Add($"Prediction-style language: \"{phrase}\"");
            }
        }

        foreach (var phrase in RecommendationPhrases)
        {
            if (lower.Contains(phrase, StringComparison.Ordinal))
            {
                violations.Add($"Recommendation-style language: \"{phrase}\"");
            }
        }

        if (FuturePredictionRegex().IsMatch(text))
        {
            violations.Add("Future-tense market claim (\"will …\")");
        }

        return violations;
    }

    /// <summary>True when the copy contains no detected rule violations.</summary>
    public static bool IsCompliant(string? text) => FindViolations(text).Count == 0;
}
