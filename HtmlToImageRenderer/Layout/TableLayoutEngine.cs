using System.Globalization;
using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Lays out table content with the automatic table layout algorithm: column widths are derived
/// from the preferred (max-content) width of the cells, then scaled so the grid exactly fills the
/// table, after which rows and cells are positioned on that grid.
/// </summary>
/// <remarks>
/// Known and accepted limitations: <c>rowspan</c> is ignored, <c>col</c>/<c>colgroup</c> are
/// ignored, and <c>border-collapse</c>/<c>border-spacing</c> are not modelled. Cells keep their own
/// borders with no spacing between them, so two adjacent cell borders meet and read as a two pixel
/// line. Cell content is always top aligned because <c>vertical-align</c> is not implemented.
/// </remarks>
public sealed class TableLayoutEngine
{
    /// <summary>
    /// Upper bound for <c>colspan</c>, which keeps malformed markup from allocating huge grids.
    /// </summary>
    private const int MaxColumnSpan = 64;

    private readonly LayoutContext _context;
    private readonly BlockLayoutEngine _block;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableLayoutEngine"/> class.
    /// </summary>
    /// <param name="context">Layout context providing styles, fonts and images.</param>
    /// <param name="block">Block engine used to lay out captions, rows and cells.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public TableLayoutEngine(LayoutContext context, BlockLayoutEngine block)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(block);

        _context = context;
        _block = block;
    }

    /// <summary>
    /// Lays out the content of a table box and fills its children with captions, rows and cells.
    /// </summary>
    /// <param name="box">Table container box, already sized horizontally.</param>
    /// <param name="contentWidth">Width of the table's content box.</param>
    /// <param name="containingWidth">Width of the containing block, used for percentages.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The content height the table needs.</returns>
    internal float LayoutTable(LayoutBox box, float contentWidth, float containingWidth, int depth)
    {
        if (box.Element is null || depth > LayoutEngine.MaxDepth)
        {
            return 0f;
        }

        var rows = new List<TableRow>();
        var aboveGrid = new List<(HtmlElement Element, ComputedStyle Style)>();
        CollectRows(box.Element, rows, aboveGrid, depth);

        // Captions and any other block content of the table are stacked above the grid so that
        // nothing inside the table silently disappears.
        float y = 0f;
        foreach ((HtmlElement element, ComputedStyle style) in aboveGrid)
        {
            LayoutBox block = _block.LayoutBlock(element, style, contentWidth, depth + 1, null, null);
            block.X = block.Margin.Left;
            block.Y = y + block.Margin.Top;
            y = block.Y + block.Height + block.Margin.Bottom;
            box.Children.Add(block);
        }

        if (rows.Count == 0)
        {
            return y;
        }

        int columnCount = AssignColumns(rows);
        if (columnCount == 0)
        {
            return y;
        }

        // Row boxes come first because their padding and border shrink the space for the grid.
        float rowInset = 0f;
        foreach (TableRow row in rows)
        {
            row.Box = CreateRowBox(row, contentWidth, containingWidth);
            rowInset = MathF.Max(rowInset, row.Box.Padding.Horizontal + row.Box.Border.Horizontal);
        }

        float gridWidth = MathF.Max(0f, contentWidth - rowInset);
        float[] columns = MeasureColumns(rows, columnCount, gridWidth, depth);
        ScaleColumns(columns, gridWidth);

        foreach (TableRow row in rows)
        {
            LayoutBox rowBox = row.Box!;
            float rowHeight = 0f;
            float cellX = 0f;

            foreach (TableCell cell in row.Cells)
            {
                cell.Width = SpanWidth(columns, cell.ColumnIndex, cell.Span, columnCount);

                LayoutBox cellBox = LayoutCell(cell, depth);
                cellBox.X = cellX + cellBox.Margin.Left;
                cellBox.Y = cellBox.Margin.Top;
                cellX += cell.Width;

                rowHeight = MathF.Max(rowHeight, cellBox.Margin.Vertical + cellBox.Height);
                rowBox.Children.Add(cellBox);
                cell.Box = cellBox;
            }

            // Every cell of a row shares the row height, which is what stretches cell borders and
            // backgrounds to a uniform grid.
            foreach (TableCell cell in row.Cells)
            {
                LayoutBox cellBox = cell.Box!;
                cellBox.Height = MathF.Max(cellBox.Height, rowHeight - cellBox.Margin.Vertical);
            }

            rowBox.Height = rowHeight + rowBox.Padding.Vertical + rowBox.Border.Vertical;
            rowBox.X = 0f;
            rowBox.Y = y;
            box.Children.Add(rowBox);
            y += rowBox.Height;
        }

        return y;
    }

    /// <summary>
    /// Measures the width a table wants when it is not forced to fill its container, which is the
    /// sum of its natural column widths.
    /// </summary>
    /// <param name="tableElement">Table element.</param>
    /// <param name="style">Computed style of the table.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The preferred border-box width in pixels.</returns>
    internal float MeasureMaxContentWidth(HtmlElement tableElement, ComputedStyle style, int depth)
    {
        if (depth > LayoutEngine.MaxDepth)
        {
            return 0f;
        }

        float extra = MathF.Max(0f, _context.ResolveLength(style.PaddingLeft, 0f, style)) +
                      MathF.Max(0f, _context.ResolveLength(style.PaddingRight, 0f, style));

        if (style.HasVisibleBorder)
        {
            extra += style.HorizontalBorderWidth;
        }

        var rows = new List<TableRow>();
        CollectRows(tableElement, rows, null, depth);

        if (rows.Count == 0)
        {
            return extra;
        }

        int columnCount = AssignColumns(rows);
        if (columnCount == 0)
        {
            return extra;
        }

        float[] columns = MeasureColumns(rows, columnCount, 0f, depth);
        float total = 0f;

        foreach (float width in columns)
        {
            total += width;
        }

        return total + extra;
    }

    /// <summary>
    /// Walks the children of the table, descending into row groups, and collects its rows.
    /// </summary>
    /// <param name="container">Table or row-group element whose children are inspected.</param>
    /// <param name="rows">List that receives the rows in document order.</param>
    /// <param name="aboveGrid">Optional list that receives captions and other non-row block content.</param>
    /// <param name="depth">Current recursion depth.</param>
    private void CollectRows(
        HtmlElement container,
        List<TableRow> rows,
        List<(HtmlElement Element, ComputedStyle Style)>? aboveGrid,
        int depth)
    {
        if (depth > LayoutEngine.MaxDepth)
        {
            return;
        }

        foreach (HtmlNode child in container.Children)
        {
            // Text between table children is not rendered, matching browser behaviour.
            if (child is not HtmlElement element)
            {
                continue;
            }

            ComputedStyle style = _context.Styles.Resolve(element);
            if (style.Display == DisplayMode.None)
            {
                continue;
            }

            switch (style.Display)
            {
                case DisplayMode.TableRow:
                    rows.Add(BuildRow(element, style));
                    break;

                case DisplayMode.TableRowGroup:
                    // thead, tbody and tfoot are transparent: their rows belong to the same grid.
                    CollectRows(element, rows, aboveGrid, depth + 1);
                    break;

                default:
                    // caption, stray cells and any other block content render above the grid.
                    aboveGrid?.Add((element, style));
                    break;
            }
        }
    }

    /// <summary>
    /// Builds the cell list of one row, turning stray inline content into an anonymous cell so it
    /// is not lost.
    /// </summary>
    /// <param name="element">Row element.</param>
    /// <param name="style">Computed style of the row.</param>
    /// <returns>The row with its cells.</returns>
    private TableRow BuildRow(HtmlElement element, ComputedStyle style)
    {
        var row = new TableRow(element, style);
        var strays = new List<HtmlNode>();

        void FlushStrays()
        {
            if (strays.Count == 0)
            {
                return;
            }

            var nodes = new List<HtmlNode>(strays);
            strays.Clear();
            row.Cells.Add(new TableCell(style, nodes, 1));
        }

        foreach (HtmlNode child in element.Children)
        {
            switch (child)
            {
                case HtmlTextNode textNode:
                    if (!WhitespaceRules.IsNullOrCollapsible(textNode.Text))
                    {
                        strays.Add(textNode);
                    }

                    break;

                case HtmlElement childElement:
                {
                    ComputedStyle childStyle = _context.Styles.Resolve(childElement);
                    if (childStyle.Display == DisplayMode.None)
                    {
                        break;
                    }

                    if (childStyle.Display == DisplayMode.TableCell)
                    {
                        FlushStrays();
                        row.Cells.Add(new TableCell(childElement, childStyle, GetSpan(childElement)));
                        break;
                    }

                    strays.Add(childElement);
                    break;
                }
            }
        }

        FlushStrays();
        return row;
    }

    /// <summary>
    /// Assigns the first column index of every cell and returns the column count of the table.
    /// </summary>
    /// <param name="rows">Rows of the table.</param>
    /// <returns>The number of columns, or zero when the table has no cells.</returns>
    private static int AssignColumns(List<TableRow> rows)
    {
        int columnCount = 0;

        foreach (TableRow row in rows)
        {
            int used = 0;

            foreach (TableCell cell in row.Cells)
            {
                cell.ColumnIndex = used;
                used += cell.Span;
            }

            columnCount = Math.Max(columnCount, used);
        }

        return columnCount;
    }

    /// <summary>
    /// Computes the natural width of every column from the preferred widths of the cells.
    /// </summary>
    /// <param name="rows">Rows of the table.</param>
    /// <param name="columnCount">Number of columns in the grid.</param>
    /// <param name="percentBase">Base used to resolve percentage widths of cells.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The unscaled column widths.</returns>
    private float[] MeasureColumns(List<TableRow> rows, int columnCount, float percentBase, int depth)
    {
        var columns = new float[columnCount];

        foreach (TableRow row in rows)
        {
            foreach (TableCell cell in row.Cells)
            {
                float preferred = MeasurePreferredWidth(cell, percentBase, depth);
                float current = SpanWidth(columns, cell.ColumnIndex, cell.Span, columnCount);

                if (preferred <= current)
                {
                    continue;
                }

                // A spanning cell spreads the missing width evenly over the columns it covers.
                float extra = (preferred - current) / cell.Span;

                for (int index = cell.ColumnIndex; index < cell.ColumnIndex + cell.Span && index < columnCount; index++)
                {
                    columns[index] += extra;
                }
            }
        }

        return columns;
    }

    /// <summary>
    /// Scales the columns so their sum equals the available width, which keeps vertical rules
    /// aligned across rows.
    /// </summary>
    /// <param name="columns">Column widths, updated in place.</param>
    /// <param name="targetWidth">Width the grid must occupy.</param>
    private static void ScaleColumns(float[] columns, float targetWidth)
    {
        if (columns.Length == 0 || targetWidth <= 0f)
        {
            return;
        }

        float total = 0f;
        foreach (float width in columns)
        {
            total += width;
        }

        if (total <= 0f)
        {
            float even = targetWidth / columns.Length;

            for (int index = 0; index < columns.Length; index++)
            {
                columns[index] = even;
            }

            return;
        }

        if (MathF.Abs(total - targetWidth) <= 0.01f)
        {
            return;
        }

        float factor = targetWidth / total;

        for (int index = 0; index < columns.Length; index++)
        {
            columns[index] *= factor;
        }
    }

    /// <summary>
    /// Measures how wide a cell wants to be, margins included.
    /// </summary>
    /// <param name="cell">Cell to measure.</param>
    /// <param name="percentBase">Base used to resolve percentage widths.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The preferred border-box width in pixels.</returns>
    private float MeasurePreferredWidth(TableCell cell, float percentBase, int depth)
    {
        if (cell.IsAnonymous)
        {
            return cell.Nodes is null ? 0f : _block.Inline.MeasureMaxContentWidth(cell.Nodes, cell.Style);
        }

        ComputedStyle style = cell.Style;
        float width = _block.MeasureMaxContentWidth(cell.Element!, style, depth + 1);
        return MathF.Max(width + ResolveHorizontalMargin(style, percentBase), 0f);
    }

    /// <summary>
    /// Creates the box of a row, resolving its own padding and border so row backgrounds and rules
    /// still paint.
    /// </summary>
    /// <param name="row">Row to create the box for.</param>
    /// <param name="contentWidth">Width of the table's content box.</param>
    /// <param name="containingWidth">Percentage base for the row's lengths.</param>
    /// <returns>The row box, not yet positioned.</returns>
    private LayoutBox CreateRowBox(TableRow row, float contentWidth, float containingWidth)
    {
        ComputedStyle style = row.Style;
        var rowBox = new LayoutBox { Element = row.Element, Style = style };

        rowBox.Padding = new BoxEdges(
            MathF.Max(0f, _context.ResolveLength(style.PaddingTop, containingWidth, style)),
            MathF.Max(0f, _context.ResolveLength(style.PaddingRight, containingWidth, style)),
            MathF.Max(0f, _context.ResolveLength(style.PaddingBottom, containingWidth, style)),
            MathF.Max(0f, _context.ResolveLength(style.PaddingLeft, containingWidth, style)));

        // A side only contributes to the row box when its own style is not None.
        rowBox.Border = new BoxEdges(
            style.ResolvedBorderTop,
            style.ResolvedBorderRight,
            style.ResolvedBorderBottom,
            style.ResolvedBorderLeft);

        // Margins are not allowed on table rows in CSS, so they are dropped instead of resolved.
        rowBox.Margin = BoxEdges.Zero;
        rowBox.Width = contentWidth;
        return rowBox;
    }

    /// <summary>
    /// Lays out one cell at its assigned width.
    /// </summary>
    /// <param name="cell">Cell to lay out.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The cell box, not yet positioned.</returns>
    private LayoutBox LayoutCell(TableCell cell, int depth)
    {
        if (!cell.IsAnonymous)
        {
            return _block.LayoutBlock(cell.Element!, cell.Style, cell.Width, depth + 1, cell.Width, null);
        }

        ComputedStyle style = CreateAnonymousStyle(cell.Style);
        var anonymous = new LayoutBox { Element = null, Style = style };
        anonymous.Width = cell.Width;
        anonymous.Height = cell.Nodes is null
            ? 0f
            : _block.Inline.LayoutContent(anonymous, cell.Nodes, cell.Width, depth + 1);

        return anonymous;
    }

    /// <summary>
    /// Creates a style for an anonymous cell: it keeps the inherited text properties but drops
    /// everything that would paint or offset the box itself.
    /// </summary>
    /// <param name="containerStyle">Style of the row.</param>
    /// <returns>A style safe for an anonymous box.</returns>
    private static ComputedStyle CreateAnonymousStyle(ComputedStyle containerStyle)
    {
        ComputedStyle style = containerStyle.Clone();
        style.Display = DisplayMode.Block;
        style.Width = CssLength.Auto;
        style.Height = CssLength.Auto;
        style.MinWidth = CssLength.Auto;
        style.MinHeight = CssLength.Auto;
        style.MaxWidth = CssLength.Auto;
        style.MaxHeight = CssLength.Auto;
        style.MarginTop = style.MarginRight = style.MarginBottom = style.MarginLeft = CssLength.Zero;
        style.PaddingTop = style.PaddingRight = style.PaddingBottom = style.PaddingLeft = CssLength.Zero;
        style.BorderTopWidth = style.BorderRightWidth = style.BorderBottomWidth = style.BorderLeftWidth = 0f;
        style.BorderTopStyle = style.BorderRightStyle = style.BorderBottomStyle = style.BorderLeftStyle = BorderStyleKind.None;
        style.BorderTopColor = style.BorderRightColor = style.BorderBottomColor = style.BorderLeftColor = CssColor.Black;
        style.BorderRadius = CssBorderRadius.None;
        style.BackgroundColor = CssColor.Transparent;
        return style;
    }

    /// <summary>
    /// Reads the <c>colspan</c> attribute of a cell.
    /// </summary>
    /// <param name="element">Cell element.</param>
    /// <returns>The clamped span, at least one column.</returns>
    private static int GetSpan(HtmlElement element)
    {
        string? value = element.GetAttribute("colspan");

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int span)
            ? Math.Clamp(span, 1, MaxColumnSpan)
            : 1;
    }

    /// <summary>
    /// Resolves the horizontal margins of a style, treating <c>auto</c> as zero.
    /// </summary>
    /// <param name="style">Style providing the margins and font size.</param>
    /// <param name="percentBase">Percentage base for the margins.</param>
    /// <returns>The combined horizontal margin in pixels.</returns>
    private float ResolveHorizontalMargin(ComputedStyle style, float percentBase)
    {
        float left = style.MarginLeft.IsAuto ? 0f : _context.ResolveLength(style.MarginLeft, percentBase, style);
        float right = style.MarginRight.IsAuto ? 0f : _context.ResolveLength(style.MarginRight, percentBase, style);
        return left + right;
    }

    /// <summary>
    /// Sums the widths of the columns covered by a spanning cell.
    /// </summary>
    /// <param name="columns">Column widths.</param>
    /// <param name="start">Index of the first covered column.</param>
    /// <param name="span">Number of covered columns.</param>
    /// <param name="columnCount">Total number of columns.</param>
    /// <returns>The combined width in pixels.</returns>
    private static float SpanWidth(float[] columns, int start, int span, int columnCount)
    {
        float width = 0f;

        for (int index = start; index < start + span && index < columnCount; index++)
        {
            width += columns[index];
        }

        return width;
    }
}
