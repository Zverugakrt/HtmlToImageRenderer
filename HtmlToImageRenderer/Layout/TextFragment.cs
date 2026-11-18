using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Resources;
using SkiaSharp;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// A positioned piece of inline content: either a run of text sharing one font, or an
/// inline image. Offsets are relative to the line the fragment belongs to.
/// </summary>
public sealed class TextFragment
{
    /// <summary>
    /// Gets the text of the fragment, or <c>null</c> when the fragment is an image.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets the image of the fragment, or <c>null</c> when the fragment is text.
    /// </summary>
    public HtmlImage? Image { get; init; }

    /// <summary>
    /// Gets the font used for text fragments. It is owned by the font cache and must not be disposed.
    /// </summary>
    public SKFont? Font { get; init; }

    /// <summary>
    /// Gets the color the fragment is painted with.
    /// </summary>
    public CssColor Color { get; init; } = CssColor.Black;

    /// <summary>
    /// Gets a value indicating whether a decoration line is drawn under the text.
    /// </summary>
    public bool Underline { get; init; }

    /// <summary>
    /// Gets the advance width of the fragment in pixels.
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// Gets the height of the fragment box in pixels.
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    /// Gets or sets the horizontal offset of the fragment inside its line.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the baseline offset from the top of the line. For image fragments this is
    /// the offset of the bottom edge, because images sit on the baseline.
    /// </summary>
    public float Baseline { get; set; }

    /// <summary>
    /// Gets a value indicating whether the fragment draws an image instead of text.
    /// </summary>
    public bool IsImage => Image is not null;
}
