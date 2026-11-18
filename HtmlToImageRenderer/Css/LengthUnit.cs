namespace HtmlToImageRenderer.Css;

/// <summary>
/// Unit of a CSS length value.
/// </summary>
public enum LengthUnit
{
    /// <summary>Keyword <c>auto</c>: the value is computed by layout.</summary>
    Auto,

    /// <summary>Absolute CSS pixels.</summary>
    Px,

    /// <summary>Percentage of the containing block width (or font size for text properties).</summary>
    Percent,

    /// <summary>Relative to the element's own font size.</summary>
    Em,

    /// <summary>Relative to the root font size.</summary>
    Rem,

    /// <summary>Typographic points, converted at 96/72 pixels per point.</summary>
    Pt,

    /// <summary>Bare number without a unit, used by <c>line-height</c> and <c>flex-grow</c>.</summary>
    Unitless,
}
