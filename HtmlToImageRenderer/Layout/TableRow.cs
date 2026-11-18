using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Book-keeping for one row of a table: the element that produced it, its computed style and
/// the cells that participate in the shared column grid.
/// </summary>
internal sealed class TableRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableRow"/> class.
    /// </summary>
    /// <param name="element">Element of the row, usually <c>tr</c>.</param>
    /// <param name="style">Resolved style of the row.</param>
    public TableRow(HtmlElement element, ComputedStyle style)
    {
        Element = element;
        Style = style;
    }

    /// <summary>
    /// Gets the element that produced the row.
    /// </summary>
    public HtmlElement Element { get; }

    /// <summary>
    /// Gets the style of the row, which supplies its own padding, border and background.
    /// </summary>
    public ComputedStyle Style { get; }

    /// <summary>
    /// Gets the cells of the row in document order.
    /// </summary>
    public List<TableCell> Cells { get; } = new();

    /// <summary>
    /// Gets or sets the laid out box of the row, whose children are the cell boxes.
    /// </summary>
    public LayoutBox? Box { get; set; }
}
