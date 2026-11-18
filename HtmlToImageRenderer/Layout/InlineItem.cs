using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Resources;
using SkiaSharp;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// One flattened piece of inline content produced while walking an inline subtree. Items are
/// the unit the inline engine wraps into lines, and each carries its own metrics so lines can
/// mix font sizes and images.
/// </summary>
internal sealed class InlineItem
{
    /// <summary>
    /// Gets the text of a text item, or <c>null</c> for images and hard breaks.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets the image of an image item, or <c>null</c> for text.
    /// </summary>
    public HtmlImage? Image { get; init; }

    /// <summary>
    /// Gets the font of a text item.
    /// </summary>
    public SKFont? Font { get; init; }

    /// <summary>
    /// Gets the color text is painted with.
    /// </summary>
    public CssColor Color { get; init; } = CssColor.Black;

    /// <summary>
    /// Gets a value indicating whether the item is underlined.
    /// </summary>
    public bool Underline { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item is a single space, which may be trimmed at a
    /// line edge and never starts a line.
    /// </summary>
    public bool IsSpace { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item forces a new line, as <c>&lt;br&gt;</c> does.
    /// </summary>
    public bool IsForcedBreak { get; init; }

    /// <summary>
    /// Gets the advance width of the item in pixels.
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// Gets the height of the item box, which is the line height for text and the image height
    /// for images.
    /// </summary>
    public float BoxHeight { get; init; }

    /// <summary>
    /// Gets the distance from the top of the item box to the baseline.
    /// </summary>
    public float Ascent { get; init; }

    /// <summary>
    /// Gets the distance from the baseline to the bottom of the item box.
    /// </summary>
    public float BelowBaseline { get; init; }
}
