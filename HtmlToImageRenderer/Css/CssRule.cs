namespace HtmlToImageRenderer.Css;

/// <summary>
/// A style rule: one or more selectors sharing a declaration block.
/// </summary>
public sealed class CssRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssRule"/> class.
    /// </summary>
    /// <param name="selectors">Selector list of the rule; must contain at least one entry.</param>
    /// <param name="declarations">Declarations of the rule, in source order.</param>
    public CssRule(IReadOnlyList<CssSelector> selectors, IReadOnlyList<CssDeclaration> declarations)
    {
        Selectors = selectors;
        Declarations = declarations;
    }

    /// <summary>
    /// Gets the selector list.
    /// </summary>
    public IReadOnlyList<CssSelector> Selectors { get; }

    /// <summary>
    /// Gets the declarations in source order.
    /// </summary>
    public IReadOnlyList<CssDeclaration> Declarations { get; }

    /// <summary>
    /// Gets the position of the rule inside its stylesheet, assigned by
    /// <see cref="CssStylesheet.Add"/>. It breaks specificity ties in the cascade:
    /// later rules win.
    /// </summary>
    public int Order { get; internal set; }
}
