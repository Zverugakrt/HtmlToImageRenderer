namespace HtmlToImageRenderer.Css;

/// <summary>
/// Relationship between two compound selectors of a single selector.
/// </summary>
public enum CssCombinator
{
    /// <summary>Right-hand side is a descendant of the left-hand side.</summary>
    Descendant,

    /// <summary>Right-hand side is a direct child of the left-hand side.</summary>
    Child,
}
