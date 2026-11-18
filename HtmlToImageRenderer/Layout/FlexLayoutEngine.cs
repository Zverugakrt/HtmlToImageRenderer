using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Lays out the children of a flex container. Single-line flexbox is implemented: items are
/// measured along the main axis, free space is distributed by <c>flex-grow</c> (or removed by
/// <c>flex-shrink</c>), then items are placed by <c>justify-content</c> and aligned across the
/// cross axis by <c>align-items</c> / <c>align-self</c>.
/// </summary>
public sealed class FlexLayoutEngine
{
    private readonly LayoutContext _context;
    private readonly BlockLayoutEngine _block;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlexLayoutEngine"/> class.
    /// </summary>
    /// <param name="context">Layout context providing styles, fonts and images.</param>
    /// <param name="block">Block engine used to lay out the individual items.</param>
    public FlexLayoutEngine(LayoutContext context, BlockLayoutEngine block)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(block);
        _context = context;
        _block = block;
    }

    /// <summary>
    /// Lays out the items of a flex container and fills its children.
    /// </summary>
    /// <param name="box">Flex container box, already sized horizontally.</param>
    /// <param name="contentWidth">Width of the container's content box.</param>
    /// <param name="definiteContentHeight">
    /// Content height when the container has a declared height, otherwise <c>null</c>.
    /// A column container without a definite height cannot distribute free space.
    /// </param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The content height the container needs.</returns>
    internal float LayoutFlexItems(LayoutBox box, float contentWidth, float? definiteContentHeight, int depth)
    {
        if (box.Element is null || depth > LayoutEngine.MaxDepth)
        {
            return 0f;
        }

        ComputedStyle containerStyle = box.Style;
        bool isRow = containerStyle.FlexDirection is FlexDirection.Row or FlexDirection.RowReverse;
        bool isReverse = containerStyle.FlexDirection is FlexDirection.RowReverse or FlexDirection.ColumnReverse;
        float gap = MathF.Max(0f, isRow ? containerStyle.ColumnGap : containerStyle.RowGap);

        var items = new List<FlexItem>();
        // Flex items are collected per child element: grouping inline children the way block flow
        // does would merge adjacent spans into a single item and break justify-content.
        foreach (InlineGroup group in FlexItemCollector.Collect(box.Element, _context.Styles))
        {
            if (group.IsAnonymous)
            {
                if (IsWhitespaceOnly(group))
                {
                    continue;
                }

                items.Add(new FlexItem(group, containerStyle));
                continue;
            }

            ComputedStyle style = group.Style ?? _context.Styles.Resolve(group.Element!);
            if (style.Display == DisplayMode.None)
            {
                continue;
            }

            items.Add(new FlexItem(group.Element!, style));
        }

        if (items.Count == 0)
        {
            return 0f;
        }

        foreach (FlexItem item in items)
        {
            item.BaseSize = MeasureBaseSize(item, isRow, contentWidth, depth);
        }

        bool canDistribute = isRow || definiteContentHeight.HasValue;
        float mainSize = isRow ? contentWidth : definiteContentHeight ?? 0f;
        float totalBase = gap * (items.Count - 1);

        foreach (FlexItem item in items)
        {
            totalBase += item.BaseSize;
        }

        DistributeSpace(items, totalBase, mainSize, canDistribute);

        // Build every item box with the main size forced on it.
        foreach (FlexItem item in items)
        {
            float marginMain = item.IsAnonymous
                ? 0f
                : isRow
                    ? ResolveMargin(item.Style, item.Style.MarginLeft, contentWidth) + ResolveMargin(item.Style, item.Style.MarginRight, contentWidth)
                    : ResolveMargin(item.Style, item.Style.MarginTop, contentWidth) + ResolveMargin(item.Style, item.Style.MarginBottom, contentWidth);

            float mainBorderSize = MathF.Max(0f, item.TargetSize - marginMain);

            item.Box = item.IsAnonymous
                ? BuildAnonymousItemBox(item, isRow, contentWidth, mainBorderSize, depth)
                : isRow
                    ? _block.LayoutBlock(item.Element!, item.Style, contentWidth, depth + 1, mainBorderSize, null)
                    : _block.LayoutBlock(item.Element!, item.Style, contentWidth, depth + 1, null, mainBorderSize);
        }

        float maxCross = 0f;
        foreach (FlexItem item in items)
        {
            LayoutBox itemBox = item.Box!;
            float cross = isRow
                ? itemBox.Margin.Vertical + itemBox.Height
                : itemBox.Margin.Horizontal + itemBox.Width;
            maxCross = MathF.Max(maxCross, cross);
        }

        float crossContainer = isRow ? definiteContentHeight ?? maxCross : contentWidth;

        // align-items: stretch grows the cross size of the item to the container's cross size.
        foreach (FlexItem item in items)
        {
            AlignItems align = item.Style.AlignSelf ?? containerStyle.AlignItems;
            if (align != AlignItems.Stretch)
            {
                continue;
            }

            LayoutBox itemBox = item.Box!;
            if (isRow)
            {
                itemBox.Height = MathF.Max(itemBox.Height, MathF.Max(0f, crossContainer - itemBox.Margin.Vertical));
            }
            else
            {
                itemBox.Width = MathF.Max(itemBox.Width, MathF.Max(0f, crossContainer - itemBox.Margin.Horizontal));
            }
        }

        float usedMain = gap * (items.Count - 1);
        foreach (FlexItem item in items)
        {
            usedMain += item.TargetSize;
        }

        float remaining = canDistribute ? MathF.Max(0f, mainSize - usedMain) : 0f;
        float start;
        float between = gap;

        switch (containerStyle.JustifyContent)
        {
            case JustifyContent.FlexEnd:
                start = remaining;
                break;
            case JustifyContent.Center:
                start = remaining / 2f;
                break;
            case JustifyContent.SpaceBetween:
                start = 0f;
                if (items.Count > 1)
                {
                    between = gap + remaining / (items.Count - 1);
                }

                break;
            case JustifyContent.SpaceAround:
            {
                float each = remaining / items.Count;
                start = each / 2f;
                between = gap + each;
                break;
            }
            case JustifyContent.SpaceEvenly:
            {
                float each = remaining / (items.Count + 1);
                start = each;
                between = gap + each;
                break;
            }
            default:
                start = 0f;
                break;
        }

        float cursor = start;
        float mainExtent = start;
        IEnumerable<FlexItem> order = isReverse ? Enumerable.Reverse(items) : items;

        foreach (FlexItem item in order)
        {
            LayoutBox itemBox = item.Box!;
            AlignItems align = item.Style.AlignSelf ?? containerStyle.AlignItems;

            if (isRow)
            {
                float crossFree = MathF.Max(0f, crossContainer - (itemBox.Height + itemBox.Margin.Vertical));
                float crossOffset = align switch
                {
                    AlignItems.FlexEnd => crossFree,
                    AlignItems.Center => crossFree / 2f,
                    _ => 0f,
                };

                itemBox.X = cursor + itemBox.Margin.Left;
                itemBox.Y = crossOffset + itemBox.Margin.Top;
            }
            else
            {
                float crossFree = MathF.Max(0f, crossContainer - (itemBox.Width + itemBox.Margin.Horizontal));
                float crossOffset = align switch
                {
                    AlignItems.FlexEnd => crossFree,
                    AlignItems.Center => crossFree / 2f,
                    _ => 0f,
                };

                itemBox.X = crossOffset + itemBox.Margin.Left;
                itemBox.Y = cursor + itemBox.Margin.Top;
            }

            cursor += item.TargetSize + between;
            mainExtent = MathF.Max(mainExtent, cursor - between);
            box.Children.Add(itemBox);
        }

        return isRow ? MathF.Max(crossContainer, maxCross) : mainExtent;
    }

    /// <summary>
    /// Applies grow and shrink factors to the base sizes.
    /// </summary>
    /// <param name="items">Items being sized.</param>
    /// <param name="totalBase">Sum of base sizes and gaps.</param>
    /// <param name="mainSize">Available main-axis size.</param>
    /// <param name="canDistribute">Whether free space may be distributed at all.</param>
    private static void DistributeSpace(List<FlexItem> items, float totalBase, float mainSize, bool canDistribute)
    {
        float freeSpace = mainSize - totalBase;

        if (!canDistribute)
        {
            foreach (FlexItem item in items)
            {
                item.TargetSize = item.BaseSize;
            }

            return;
        }

        if (freeSpace > 0f)
        {
            float totalGrow = 0f;
            foreach (FlexItem item in items)
            {
                totalGrow += MathF.Max(0f, item.Style.FlexGrow);
            }

            foreach (FlexItem item in items)
            {
                float grow = MathF.Max(0f, item.Style.FlexGrow);
                item.TargetSize = totalGrow > 0f
                    ? item.BaseSize + (freeSpace * (grow / totalGrow))
                    : item.BaseSize;
            }

            return;
        }

        if (freeSpace < 0f)
        {
            float totalShrink = 0f;
            foreach (FlexItem item in items)
            {
                totalShrink += MathF.Max(0f, item.Style.FlexShrink) * item.BaseSize;
            }

            foreach (FlexItem item in items)
            {
                float weight = MathF.Max(0f, item.Style.FlexShrink) * item.BaseSize;
                float share = totalShrink > 0f ? weight / totalShrink : 0f;
                item.TargetSize = MathF.Max(item.BaseSize + (freeSpace * share), 0f);
            }

            return;
        }

        foreach (FlexItem item in items)
        {
            item.TargetSize = item.BaseSize;
        }
    }

    /// <summary>
    /// Measures the flex base size of an item, margins included.
    /// </summary>
    /// <param name="item">Item to measure.</param>
    /// <param name="isRow">Whether the main axis is horizontal.</param>
    /// <param name="contentWidth">Container content width, used for percentage resolution.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The base size in pixels.</returns>
    private float MeasureBaseSize(FlexItem item, bool isRow, float contentWidth, int depth)
    {
        ComputedStyle style = item.Style;
        float marginMain = item.IsAnonymous
            ? 0f
            : isRow
                ? ResolveMargin(style, style.MarginLeft, contentWidth) + ResolveMargin(style, style.MarginRight, contentWidth)
                : ResolveMargin(style, style.MarginTop, contentWidth) + ResolveMargin(style, style.MarginBottom, contentWidth);

        bool hasBasis = !item.IsAnonymous && !style.FlexBasis.IsAuto;
        if (hasBasis)
        {
            return MathF.Max(0f, _context.ResolveLength(style.FlexBasis, contentWidth, style)) + marginMain;
        }

        if (item.IsAnonymous)
        {
            if (isRow)
            {
                return _block.Inline.MeasureMaxContentWidth(item.Group!.Nodes, style) + marginMain;
            }

            var probe = new LayoutBox { Style = style };
            float probeHeight = _block.Inline.LayoutContent(probe, item.Group!.Nodes, contentWidth, depth + 1);
            return probeHeight + marginMain;
        }

        if (!isRow)
        {
            // A column item's intrinsic height is whatever its content needs.
            LayoutBox natural = _block.LayoutBlock(item.Element!, style, contentWidth, depth + 1, null, null);
            return natural.Height + marginMain;
        }

        if (!style.Width.IsAuto)
        {
            LayoutBox probe = _block.LayoutBlock(item.Element!, style, contentWidth, depth + 1, null, null);
            return probe.Width + marginMain;
        }

        float intrinsic = _block.MeasureMaxContentWidth(item.Element!, style, depth + 1);
        return MathF.Min(MathF.Max(intrinsic, 0f), contentWidth) + marginMain;
    }

    /// <summary>
    /// Lays out an anonymous inline group as a stand-alone flex item.
    /// </summary>
    /// <param name="item">Anonymous item.</param>
    /// <param name="isRow">Whether the main axis is horizontal.</param>
    /// <param name="contentWidth">Container content width.</param>
    /// <param name="mainBorderSize">Main-axis border-box size assigned by the flex algorithm.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <returns>The laid out box.</returns>
    private LayoutBox BuildAnonymousItemBox(FlexItem item, bool isRow, float contentWidth, float mainBorderSize, int depth)
    {
        ComputedStyle style = CreateAnonymousStyle(item.Style);
        var anonymous = new LayoutBox { Element = null, Style = style };
        anonymous.Width = isRow ? mainBorderSize : contentWidth;

        float height = _block.Inline.LayoutContent(anonymous, item.Group!.Nodes, anonymous.Width, depth + 1);
        anonymous.Height = isRow ? height : MathF.Max(height, mainBorderSize);
        return anonymous;
    }

    /// <summary>
    /// Creates a style for an anonymous box: it keeps the inherited text properties of the
    /// container but drops everything that would paint or offset the box itself.
    /// </summary>
    /// <param name="containerStyle">Style of the flex container.</param>
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
    /// Resolves one margin side, treating <c>auto</c> as zero.
    /// </summary>
    /// <param name="style">Style providing the font size.</param>
    /// <param name="margin">Margin length.</param>
    /// <param name="contentWidth">Percentage base.</param>
    /// <returns>The margin in pixels.</returns>
    private float ResolveMargin(ComputedStyle style, CssLength margin, float contentWidth) =>
        margin.IsAuto ? 0f : _context.ResolveLength(margin, contentWidth, style);

    /// <summary>
    /// Determines whether an anonymous group contains nothing but white space, in which case it
    /// must not generate an item (it would add stray gaps between block children).
    /// </summary>
    /// <param name="group">Group to inspect.</param>
    /// <returns><c>true</c> when the group only holds white-space text nodes.</returns>
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

            if (node is HtmlTextNode text && !string.IsNullOrWhiteSpace(text.Text))
            {
                return false;
            }
        }

        return true;
    }
}
