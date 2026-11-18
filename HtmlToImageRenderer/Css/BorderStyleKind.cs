namespace HtmlToImageRenderer.Css;

/// <summary>
/// Border line style. All four sides share one style; only the widths can differ per side.
/// </summary>
public enum BorderStyleKind
{
    /// <summary>No border is painted.</summary>
    None,

    /// <summary>Continuous line.</summary>
    Solid,

    /// <summary>Dashed line.</summary>
    Dashed,

    /// <summary>Dotted line.</summary>
    Dotted,
}
