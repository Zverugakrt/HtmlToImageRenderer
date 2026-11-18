using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Css;

/// <summary>
/// A complete selector: a chain of compound selectors joined by descendant or child
/// combinators, together with the specificity used by the cascade.
/// </summary>
public sealed class CssSelector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssSelector"/> class.
    /// </summary>
    /// <param name="compounds">Compound selectors ordered left to right.</param>
    /// <param name="combinators">Combinators between the compounds; length is <c>compounds.Count - 1</c>.</param>
    /// <param name="text">Original selector text, kept for diagnostics.</param>
    public CssSelector(IReadOnlyList<CssCompoundSelector> compounds, IReadOnlyList<CssCombinator> combinators, string text)
    {
        Compounds = compounds;
        Combinators = combinators;
        Text = text;

        var specificity = CssSpecificity.Zero;
        foreach (CssCompoundSelector compound in compounds)
        {
            specificity = specificity.Add(compound.GetSpecificity());
        }

        Specificity = specificity;
    }

    /// <summary>
    /// Gets the compound selectors ordered left to right.
    /// </summary>
    public IReadOnlyList<CssCompoundSelector> Compounds { get; }

    /// <summary>
    /// Gets the combinators that join adjacent compounds.
    /// </summary>
    public IReadOnlyList<CssCombinator> Combinators { get; }

    /// <summary>
    /// Gets the original selector text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the computed specificity of the whole selector.
    /// </summary>
    public CssSpecificity Specificity { get; }

    /// <summary>
    /// Parses a selector such as <c>div.card &gt; p span</c>.
    /// </summary>
    /// <param name="text">Selector text; surrounding white space is ignored.</param>
    /// <returns>The parsed selector.</returns>
    public static CssSelector Parse(string text)
    {
        var compounds = new List<CssCompoundSelector>();
        var combinators = new List<CssCombinator>();
        bool childPending = false;
        int index = 0;

        while (index < text.Length)
        {
            char current = text[index];

            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (current == '>')
            {
                childPending = true;
                index++;
                continue;
            }

            int start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] != '>')
            {
                index++;
            }

            string fragment = text[start..index];
            if (fragment.Length == 0)
            {
                index++;
                continue;
            }

            if (compounds.Count > 0)
            {
                combinators.Add(childPending ? CssCombinator.Child : CssCombinator.Descendant);
            }

            childPending = false;
            compounds.Add(CssCompoundSelector.Parse(fragment));
        }

        return new CssSelector(compounds, combinators, text);
    }

    /// <summary>
    /// Determines whether the selector matches the given element, walking ancestors as
    /// required by the combinators.
    /// </summary>
    /// <param name="element">Element to test.</param>
    /// <returns><c>true</c> when the selector matches.</returns>
    public bool Matches(HtmlElement element) =>
        Compounds.Count > 0 && MatchesAt(element, Compounds.Count - 1);

    /// <summary>
    /// Matches the compound at <paramref name="index"/> and, recursively, everything to its left.
    /// </summary>
    /// <param name="element">Element currently being tested.</param>
    /// <param name="index">Index of the compound to test.</param>
    /// <returns><c>true</c> when the whole left-hand side matches.</returns>
    private bool MatchesAt(HtmlElement element, int index)
    {
        if (!Compounds[index].Matches(element))
        {
            return false;
        }

        if (index == 0)
        {
            return true;
        }

        if (Combinators[index - 1] == CssCombinator.Child)
        {
            HtmlElement? parent = element.Parent;
            return parent is not null && MatchesAt(parent, index - 1);
        }

        for (HtmlElement? ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (MatchesAt(ancestor, index - 1))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Text;
}
