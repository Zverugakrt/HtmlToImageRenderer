namespace HtmlToImageRenderer.Css;

/// <summary>
/// Distribution of free space along a flex container's main axis
/// (value of the <c>justify-content</c> property).
/// </summary>
public enum JustifyContent
{
    /// <summary>Pack items at the start of the main axis.</summary>
    FlexStart,

    /// <summary>Pack items at the end of the main axis.</summary>
    FlexEnd,

    /// <summary>Pack items in the center of the main axis.</summary>
    Center,

    /// <summary>First item at the start, last at the end, equal space between items.</summary>
    SpaceBetween,

    /// <summary>Equal space around each item, so the outer gaps are half sized.</summary>
    SpaceAround,

    /// <summary>Equal space between items and at both ends.</summary>
    SpaceEvenly,
}
