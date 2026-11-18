namespace HtmlToImageRenderer.Css;

/// <summary>
/// White-space handling for inline content (value of the <c>white-space</c> property).
/// </summary>
public enum WhiteSpaceKind
{
    /// <summary>Collapse runs of white space and wrap lines.</summary>
    Normal,

    /// <summary>Collapse white space but never wrap.</summary>
    NoWrap,

    /// <summary>Preserve white space and line breaks, never wrap.</summary>
    Pre,

    /// <summary>Preserve white space and line breaks, but wrap when needed.</summary>
    PreWrap,
}
