namespace HtmlToImageRenderer.Css;

/// <summary>
/// Identifies which sides of a box a border shorthand applies to.
/// </summary>
[Flags]
public enum BorderSides
{
    /// <summary>No side at all.</summary>
    None = 0,

    /// <summary>The top side.</summary>
    Top = 1,

    /// <summary>The right side.</summary>
    Right = 2,

    /// <summary>The bottom side.</summary>
    Bottom = 4,

    /// <summary>The left side.</summary>
    Left = 8,

    /// <summary>All four sides.</summary>
    All = Top | Right | Bottom | Left,
}
