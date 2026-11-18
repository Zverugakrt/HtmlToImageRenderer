namespace HtmlToImageRenderer.Css;

/// <summary>
/// An ordered collection of style rules. The order is the source order and is used as the
/// final tie-breaker of the cascade.
/// </summary>
public sealed class CssStylesheet
{
    private readonly List<CssRule> _rules = new();

    /// <summary>
    /// Gets the rules in source order.
    /// </summary>
    public IReadOnlyList<CssRule> Rules => _rules;

    /// <summary>
    /// Gets the number of rules in the sheet.
    /// </summary>
    public int RuleCount => _rules.Count;

    /// <summary>
    /// Gets a new, empty stylesheet.
    /// </summary>
    public static CssStylesheet Empty => new();

    /// <summary>
    /// Appends a rule and stamps it with its position in the sheet.
    /// </summary>
    /// <param name="rule">Rule to append; must not be <c>null</c>.</param>
    public void Add(CssRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        rule.Order = _rules.Count;
        _rules.Add(rule);
    }
}
