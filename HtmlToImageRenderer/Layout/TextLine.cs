namespace HtmlToImageRenderer.Layout;

/// <summary>
/// One line of inline content: the fragments placed on it, its measured size and the
/// offset at which it is painted inside the containing block.
/// </summary>
public sealed class TextLine
{
    /// <summary>
    /// Gets the fragments of the line in visual order.
    /// </summary>
    public List<TextFragment> Fragments { get; } = new();

    /// <summary>
    /// Gets or sets the used width of the line, which is the sum of its fragments.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the line box, driven by the tallest fragment and its baseline.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets or sets the baseline offset from the top of the line box.
    /// </summary>
    public float Baseline { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset produced by <c>text-align</c>.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset of the line from the top of the containing block.
    /// </summary>
    public float Top { get; set; }
}
