using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;
using HtmlToImageRenderer.Resources;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Lays out block-level content: resolves the box model edges, sizes the box, stacks
/// block-level children vertically and hands runs of inline content to
/// <see cref="InlineLayoutEngine"/>. Vertical margins do not collapse, which keeps the model
/// predictable for generated documents.
/// </summary>
public sealed class BlockLayoutEngine
{
    private readonly LayoutContext _context;
    private readonly InlineLayoutEngine _inline;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockLayoutEngine"/> class.
    /// </summary>
    /// <param name="context">Layout context providing styles, fonts and images.</param>
    public BlockLayoutEngine(LayoutContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        _inline = new InlineLayoutEngine(context);
    }

    /// <summary>
    /// Gets the inline engine, which is shared with the flex engine so anonymous boxes are
    /// measured the same way everywhere.
    /// </summary>
    internal InlineLayoutEngine Inline => _inline;

    /// <summary>
    /// Gets or sets the flex engine. It is injected after construction because the two engines
    /// reference each other.
    /// </summary>
    internal FlexLayoutEngine? Flex { get; set; }

    /// <summary>
    /// Gets or sets the table engine. It is injected after construction for the same reason as
    /// the flex engine: table cells are laid out as block boxes.
    /// </summary>
    internal TableLayoutEngine? Table { get; set; }

    /// <summary>
    /// Lays out an element as the root of the document, using the viewport width.
    /// </summary>
    /// <param name="element">Root element, usually <c>&lt;body&gt;</c>.</param>
    /// <returns>The root box with margins applied to its own offset.</returns>
    public LayoutBox Layout(HtmlElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return LayoutBlock(element, _context.Styles.Resolve(element), _context.ViewportWidth, 0, null, null);
    }

    /// <summary>
    /// Lays out a single box with the block box model.
    /// </summary>
    /// <param name="element">Element of the box, or <c>null</c> for an anonymous box.</param>
    /// <param name="style">Computed style of the box.</param>
    /// <param name="containingContentWidth">Width of the containing block's content box.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <param name="forcedBorderBoxWidth">Main-axis width forced by a flex container, if any.</param>
    /// <param name="forcedBorderBoxHeight">Cross-axis height forced by a flex container, if any.</param>
    /// <returns>The sized and positioned box.</returns>
    internal LayoutBox LayoutBlock(
        HtmlElement? element,
        ComputedStyle style,
        float containingContentWidth,
        int depth,
        float? forcedBorderBoxWidth,
        float? forcedBorderBoxHeight)
    {
        var box = new LayoutBox { Element = element, Style = style };

        if (depth > LayoutEngine.MaxDepth)
        {
            return box;
        }

        float containingWidth = MathF.Max(0f, containingContentWidth);
        ResolveEdges(box, style, containingWidth);

        float extraHorizontal = box.Padding.Horizontal + box.Border.Horizontal;
        BoxEdges margin = box.Margin;
        float available = MathF.Max(0f, containingWidth - margin.Horizontal);

        bool isImage = false;
        float imageWidth = 0f;
        float imageHeight = 0f;
        HtmlImage? image = null;

        if (element is not null && string.Equals(element.TagName, "img", StringComparison.Ordinal))
        {
            isImage = ImageSizing.TryGetSize(_context, element, style, containingWidth, out imageWidth, out imageHeight);
            if (isImage)
            {
                image = _context.GetImage(element.GetAttribute("src"));
            }
        }

        float width;
        if (forcedBorderBoxWidth.HasValue)
        {
            width = forcedBorderBoxWidth.Value;
        }
        else if (!style.Width.IsAuto)
        {
            width = ResolveDeclaredWidth(style, containingWidth, extraHorizontal);
        }
        else if (isImage)
        {
            width = imageWidth + extraHorizontal;
        }
        else if (style.ShrinkToFitWidth && element is not null)
        {
            // Sizing to the max-content width is what makes auto margins meaningful for a table;
            // the clamp keeps the box inside its containing block.
            float preferred = MeasureMaxContentWidth(element, style, depth);
            width = MathF.Min(MathF.Max(preferred, 0f), available);
        }
        else
        {
            width = available;
        }

        box.Width = ClampSize(width, style.MinWidth, style.MaxWidth, containingWidth, style);

        // CSS resolves auto margins against the free space of the containing block: two auto margins
        // centre the box, a single one pushes it to the opposite edge. An auto-width box leaves no
        // free space, so only boxes narrower than their container are affected.
        if (!forcedBorderBoxWidth.HasValue && (style.MarginLeft.IsAuto || style.MarginRight.IsAuto))
        {
            float free = MathF.Max(0f, containingWidth - box.Width);

            if (style.MarginLeft.IsAuto && style.MarginRight.IsAuto)
            {
                margin.Left = free / 2f;
                margin.Right = free / 2f;
            }
            else if (style.MarginLeft.IsAuto)
            {
                margin.Left = free;
                margin.Right = 0f;
            }
            else
            {
                margin.Left = 0f;
                margin.Right = free;
            }

            box.Margin = margin;
        }

        float contentWidth = MathF.Max(0f, box.Width - extraHorizontal);
        float contentHeight;

        if (isImage)
        {
            box.Image = image;
            contentHeight = imageHeight;
        }
        else
        {
            contentHeight = LayoutChildren(box, contentWidth, containingWidth, depth);
        }

        float height;
        if (forcedBorderBoxHeight.HasValue)
        {
            height = forcedBorderBoxHeight.Value;
        }
        else if (!style.Height.IsAuto)
        {
            float declared = _context.ResolveLength(style.Height, containingWidth, style);
            height = style.UseBorderBox ? declared : declared + box.Padding.Vertical + box.Border.Vertical;
        }
        else
        {
            height = contentHeight + box.Padding.Vertical + box.Border.Vertical;
        }

        box.Height = ClampSize(height, style.MinHeight, style.MaxHeight, containingWidth, style);
        return box;
    }

    /// <summary>
    /// Measures the width an element would like to have when it is not forced to fill its
    /// container. This is the max-content width used for flex items and shrink-to-fit boxes.
    /// </summary>
    /// <param name="element">Element to measure.</param>
    /// <param name="style">Computed style of the element.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The preferred border-box width in pixels.</returns>
    internal float MeasureMaxContentWidth(HtmlElement element, ComputedStyle style, int depth)
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

        if (!style.Width.IsAuto)
        {
            float declared = _context.ResolveLength(style.Width, 0f, style);
            return style.UseBorderBox ? declared : declared + extra;
        }

        if (style.Display == DisplayMode.Table && Table is not null)
        {
            // A table's preferred width is the sum of its natural column widths.
            return Table.MeasureMaxContentWidth(element, style, depth);
        }

        if (string.Equals(element.TagName, "img", StringComparison.Ordinal) &&
            ImageSizing.TryGetSize(_context, element, style, 0f, out float imageWidth, out _))
        {
            return imageWidth + extra;
        }

        float widest = 0f;
        foreach (InlineGroup group in ChildGroupCollector.Collect(element, _context.Styles))
        {
            if (group.IsAnonymous)
            {
                if (IsWhitespaceOnly(group))
                {
                    continue;
                }

                widest = MathF.Max(widest, _inline.MeasureMaxContentWidth(group.Nodes, style));
                continue;
            }

            ComputedStyle childStyle = group.Style ?? _context.Styles.Resolve(group.Element!);
            if (childStyle.Display == DisplayMode.None)
            {
                continue;
            }

            float childWidth = MeasureMaxContentWidth(group.Element!, childStyle, depth + 1) +
                               ResolveHorizontalMargin(childStyle);
            widest = MathF.Max(widest, childWidth);
        }

        return widest + extra;
    }

    /// <summary>
    /// Lays out the children of a box according to its formatting context.
    /// </summary>
    /// <param name="box">Box whose children are laid out.</param>
    /// <param name="contentWidth">Width of the content box.</param>
    /// <param name="containingWidth">Width of the containing block, used for percentages.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The content height the children need.</returns>
    private float LayoutChildren(LayoutBox box, float contentWidth, float containingWidth, int depth)
    {
        if (box.Element is null)
        {
            return 0f;
        }

        if (box.Style.Display == DisplayMode.Flex && Flex is not null)
        {
            float? definiteContentHeight = null;

            if (!box.Style.Height.IsAuto)
            {
                float declared = _context.ResolveLength(box.Style.Height, containingWidth, box.Style);
                float inner = box.Style.UseBorderBox
                    ? declared - box.Padding.Vertical - box.Border.Vertical
                    : declared;
                definiteContentHeight = MathF.Max(0f, inner);
            }

            return Flex.LayoutFlexItems(box, contentWidth, definiteContentHeight, depth);
        }

        if (box.Style.Display == DisplayMode.Table && Table is not null)
        {
            return Table.LayoutTable(box, contentWidth, containingWidth, depth);
        }

        return LayoutBlockFlow(box, contentWidth, depth);
    }

    /// <summary>
    /// Stacks block-level children and anonymous inline groups vertically.
    /// </summary>
    /// <param name="box">Container box.</param>
    /// <param name="contentWidth">Width of the content box.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The total height of the stacked content.</returns>
    private float LayoutBlockFlow(LayoutBox box, float contentWidth, int depth)
    {
        List<InlineGroup> groups = ChildGroupCollector.Collect(box.Element!, _context.Styles);
        float y = 0f;

        foreach (InlineGroup group in groups)
        {
            if (group.IsAnonymous)
            {
                if (IsWhitespaceOnly(group))
                {
                    continue;
                }

                LayoutBox anonymous = CreateAnonymousBox(box.Style);
                anonymous.Width = contentWidth;

                float height = _inline.LayoutContent(anonymous, group.Nodes, contentWidth, depth + 1);
                anonymous.Height = height;
                anonymous.X = 0f;
                anonymous.Y = y;

                box.Children.Add(anonymous);
                y += height;
                continue;
            }

            ComputedStyle childStyle = group.Style ?? _context.Styles.Resolve(group.Element!);
            if (childStyle.Display == DisplayMode.None)
            {
                continue;
            }

            LayoutBox child = LayoutBlock(group.Element!, childStyle, contentWidth, depth + 1, null, null);
            child.X = child.Margin.Left;
            child.Y = y + child.Margin.Top;
            y = child.Y + child.Height + child.Margin.Bottom;
            box.Children.Add(child);
        }

        return y;
    }

    /// <summary>
    /// Resolves the margin, padding and border edges of a box.
    /// </summary>
    /// <param name="box">Box receiving the edges.</param>
    /// <param name="style">Computed style providing the declarations.</param>
    /// <param name="containingWidth">Percentage base for the lengths.</param>
    private void ResolveEdges(LayoutBox box, ComputedStyle style, float containingWidth)
    {
        box.Padding = new BoxEdges(
            MathF.Max(0f, _context.ResolveLength(style.PaddingTop, containingWidth, style)),
            MathF.Max(0f, _context.ResolveLength(style.PaddingRight, containingWidth, style)),
            MathF.Max(0f, _context.ResolveLength(style.PaddingBottom, containingWidth, style)),
            MathF.Max(0f, _context.ResolveLength(style.PaddingLeft, containingWidth, style)));

        // A side only contributes to the box model when its own style is not None.
        box.Border = new BoxEdges(
            style.ResolvedBorderTop,
            style.ResolvedBorderRight,
            style.ResolvedBorderBottom,
            style.ResolvedBorderLeft);

        box.Margin = new BoxEdges(
            style.MarginTop.IsAuto ? 0f : _context.ResolveLength(style.MarginTop, containingWidth, style),
            style.MarginRight.IsAuto ? 0f : _context.ResolveLength(style.MarginRight, containingWidth, style),
            style.MarginBottom.IsAuto ? 0f : _context.ResolveLength(style.MarginBottom, containingWidth, style),
            style.MarginLeft.IsAuto ? 0f : _context.ResolveLength(style.MarginLeft, containingWidth, style));
    }

    /// <summary>
    /// Resolves a declared width into a border-box width.
    /// </summary>
    /// <param name="style">Style providing the declared width and box sizing.</param>
    /// <param name="containingWidth">Percentage base.</param>
    /// <param name="extraHorizontal">Padding and border that <c>content-box</c> adds on top.</param>
    /// <returns>The border-box width in pixels.</returns>
    private float ResolveDeclaredWidth(ComputedStyle style, float containingWidth, float extraHorizontal)
    {
        float declared = _context.ResolveLength(style.Width, containingWidth, style);
        return style.UseBorderBox ? declared : declared + extraHorizontal;
    }

    /// <summary>
    /// Applies the minimum and maximum size constraints of a style.
    /// </summary>
    /// <param name="value">Size to clamp.</param>
    /// <param name="min">Declared minimum, or <c>auto</c>.</param>
    /// <param name="max">Declared maximum, or <c>auto</c>.</param>
    /// <param name="percentageBase">Base used to resolve percentage constraints.</param>
    /// <param name="style">Style providing the font size for relative constraints.</param>
    /// <returns>The clamped, non-negative size.</returns>
    private float ClampSize(float value, CssLength min, CssLength max, float percentageBase, ComputedStyle style)
    {
        float resolved = value;

        if (!min.IsAuto)
        {
            resolved = MathF.Max(resolved, _context.ResolveLength(min, percentageBase, style));
        }

        if (!max.IsAuto)
        {
            resolved = MathF.Min(resolved, _context.ResolveLength(max, percentageBase, style));
        }

        return MathF.Max(0f, resolved);
    }

    /// <summary>
    /// Resolves the horizontal margins of a style, treating <c>auto</c> as zero.
    /// </summary>
    /// <param name="style">Style providing the margins and font size.</param>
    /// <returns>The combined horizontal margin in pixels.</returns>
    private float ResolveHorizontalMargin(ComputedStyle style)
    {
        float left = style.MarginLeft.IsAuto ? 0f : _context.ResolveLength(style.MarginLeft, 0f, style);
        float right = style.MarginRight.IsAuto ? 0f : _context.ResolveLength(style.MarginRight, 0f, style);
        return left + right;
    }

    /// <summary>
    /// Creates a style for an anonymous box that holds inline content: it keeps the inherited
    /// text properties of the container but drops anything that would paint or offset the box.
    /// </summary>
    /// <param name="containerStyle">Style of the containing block.</param>
    /// <returns>An anonymous box with zero edges that holds inline content.</returns>
    private static LayoutBox CreateAnonymousBox(ComputedStyle containerStyle)
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

        return new LayoutBox { Element = null, Style = style };
    }

    /// <summary>
    /// Determines whether an anonymous group holds only white space, in which case it must not
    /// produce a line box (browsers collapse it away between block children).
    /// </summary>
    /// <param name="group">Group to inspect.</param>
    /// <returns><c>true</c> when the group contains only white-space text nodes.</returns>
    private static bool IsWhitespaceOnly(InlineGroup group)
    {
        if (!group.IsAnonymous)
        {
            return false;
        }

        foreach (HtmlNode node in group.Nodes)
        {
            if (node is HtmlElement)
            {
                return false;
            }

            // A no-break space is content, so a node made of them must not be dropped.
            if (node is HtmlTextNode text && !WhitespaceRules.IsNullOrCollapsible(text.Text))
            {
                return false;
            }
        }

        return true;
    }
}
