namespace HtmlToImageRenderer.Css;

/// <summary>
/// Horizontal alignment of inline content inside its block container
/// (value of the <c>text-align</c> property).
/// </summary>
public enum TextAlign
{
    /// <summary>Align to the left edge.</summary>
    Left,

    /// <summary>Center each line inside the content box.</summary>
    Center,

    /// <summary>Align to the right edge.</summary>
    Right,

    /// <summary>Requested justification. Rendered as left-aligned, since lines are not re-spaced.</summary>
    Justify,
}
