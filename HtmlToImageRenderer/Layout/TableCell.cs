using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Book-keeping for one table cell: the element (or anonymous inline content) it represents,
/// the number of columns it spans and the geometry assigned by the table algorithm.
/// </summary>
internal sealed class TableCell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableCell"/> class for a real cell element.
    /// </summary>
    /// <param name="element">Element of the cell, usually <c>td</c> or <c>th</c>.</param>
    /// <param name="style">Resolved style of the cell.</param>
    /// <param name="span">Number of columns the cell spans; at least one.</param>
    public TableCell(HtmlElement element, ComputedStyle style, int span)
    {
        Element = element;
        Style = style;
        Span = span;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableCell"/> class for stray inline content
    /// found directly inside a row.
    /// </summary>
    /// <param name="style">Style inherited from the row.</param>
    /// <param name="nodes">Inline nodes the anonymous cell contains.</param>
    /// <param name="span">Number of columns the cell spans; at least one.</param>
    public TableCell(ComputedStyle style, IReadOnlyList<HtmlNode> nodes, int span)
    {
        Style = style;
        Nodes = nodes;
        Span = span;
    }

    /// <summary>
    /// Gets the element of the cell, or <c>null</c> for an anonymous cell.
    /// </summary>
    public HtmlElement? Element { get; }

    /// <summary>
    /// Gets the inline content of an anonymous cell, or <c>null</c> for a real cell.
    /// </summary>
    public IReadOnlyList<HtmlNode>? Nodes { get; }

    /// <summary>
    /// Gets the style driving the cell.
    /// </summary>
    public ComputedStyle Style { get; }

    /// <summary>
    /// Gets the number of columns the cell spans.
    /// </summary>
    public int Span { get; }

    /// <summary>
    /// Gets a value indicating whether the cell wraps inline content instead of an element.
    /// </summary>
    public bool IsAnonymous => Element is null;

    /// <summary>
    /// Gets or sets the index of the first column the cell occupies.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Gets or sets the border-box width assigned by the column grid, in pixels.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets or sets the laid out box of the cell.
    /// </summary>
    public LayoutBox? Box { get; set; }
}
