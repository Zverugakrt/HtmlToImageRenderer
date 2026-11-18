using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;
using SkiaSharp;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Lays out inline content: it flattens inline subtrees into measurable items, wraps them into
/// line boxes, aligns the lines and aligns every fragment on the shared baseline.
/// </summary>
public sealed class InlineLayoutEngine
{
    private readonly LayoutContext _context;
    private readonly LineBreaker _lineBreaker;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineLayoutEngine"/> class.
    /// </summary>
    /// <param name="context">Layout context providing styles, fonts and images.</param>
    public InlineLayoutEngine(LayoutContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        _lineBreaker = new LineBreaker(context.Fonts);
    }

    /// <summary>
    /// Lays out a run of inline nodes inside a box and fills <see cref="LayoutBox.Lines"/>.
    /// </summary>
    /// <param name="box">
    /// Box that receives the lines; its style provides alignment, white-space and font defaults.
    /// </param>
    /// <param name="nodes">Inline-level nodes in document order.</param>
    /// <param name="availableWidth">Width of the content box the lines may use.</param>
    /// <param name="depth">Current recursion depth, used to stop pathological documents.</param>
    /// <returns>The total height of the produced line boxes.</returns>
    internal float LayoutContent(LayoutBox box, IReadOnlyList<HtmlNode> nodes, float availableWidth, int depth)
    {
        var items = new List<InlineItem>();
        CollectItems(nodes, box.Style, availableWidth, items, depth);

        if (items.Count == 0)
        {
            return 0f;
        }

        List<TextLine> lines = BuildLines(items, availableWidth, box.Style);
        float y = 0f;

        foreach (TextLine line in lines)
        {
            line.Top = y;
            line.X = AlignLine(line, availableWidth, box.Style.TextAlign);
            y += line.Height;
            box.Lines.Add(line);
        }

        return y;
    }

    /// <summary>
    /// Measures the widest unbroken run of inline content, which is the max-content width used
    /// when sizing flex items and shrink-to-fit boxes.
    /// </summary>
    /// <param name="nodes">Inline-level nodes to measure.</param>
    /// <param name="style">Style providing fonts and white-space handling.</param>
    /// <returns>The widest segment between hard breaks, in pixels.</returns>
    internal float MeasureMaxContentWidth(IReadOnlyList<HtmlNode> nodes, ComputedStyle style)
    {
        var items = new List<InlineItem>();
        CollectItems(nodes, style, 0f, items, 0);

        float widest = 0f;
        float current = 0f;

        foreach (InlineItem item in items)
        {
            if (item.IsForcedBreak)
            {
                widest = MathF.Max(widest, current);
                current = 0f;
                continue;
            }

            current += item.Width;
        }

        return MathF.Max(widest, current);
    }

    /// <summary>
    /// Flattens inline nodes into items, resolving styles for nested inline elements.
    /// </summary>
    /// <param name="nodes">Nodes to flatten.</param>
    /// <param name="parentStyle">Style inherited by text nodes at this level.</param>
    /// <param name="percentBase">Base used to resolve percentage image sizes.</param>
    /// <param name="items">List that receives the produced items.</param>
    /// <param name="depth">Current recursion depth.</param>
    private void CollectItems(
        IReadOnlyList<HtmlNode> nodes,
        ComputedStyle parentStyle,
        float percentBase,
        List<InlineItem> items,
        int depth)
    {
        if (depth > LayoutEngine.MaxDepth)
        {
            return;
        }

        foreach (HtmlNode node in nodes)
        {
            switch (node)
            {
                case HtmlTextNode textNode:
                    AppendText(textNode.Text, parentStyle, items);
                    break;

                case HtmlElement element:
                {
                    ComputedStyle style = _context.Styles.Resolve(element);
                    if (style.Display == DisplayMode.None)
                    {
                        break;
                    }

                    if (string.Equals(element.TagName, "br", StringComparison.Ordinal))
                    {
                        items.Add(new InlineItem { IsForcedBreak = true });
                        break;
                    }

                    // Table structure behaves like a block here: a line cannot contain a grid.
                    if (style.Display is DisplayMode.Block or DisplayMode.Flex or DisplayMode.Table
                        or DisplayMode.TableRow or DisplayMode.TableRowGroup or DisplayMode.TableCell)
                    {
                        // A block box cannot live inside a line box. It breaks the line and its
                        // content is flattened so nothing is dropped from the output.
                        items.Add(new InlineItem { IsForcedBreak = true });
                        CollectItems(element.Children, style, percentBase, items, depth + 1);
                        items.Add(new InlineItem { IsForcedBreak = true });
                        break;
                    }

                    if (string.Equals(element.TagName, "img", StringComparison.Ordinal))
                    {
                        AppendImage(element, style, percentBase, items);
                        break;
                    }

                    CollectItems(element.Children, style, percentBase, items, depth + 1);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Tokenizes a text run and turns every token into an item with measured font metrics.
    /// </summary>
    /// <param name="text">Text to append.</param>
    /// <param name="style">Style of the text, providing font, color and white-space handling.</param>
    /// <param name="items">List that receives the produced items.</param>
    private void AppendText(string? text, ComputedStyle style, List<InlineItem> items)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SKFont font = _context.Fonts.GetFont(style);
        float ascent = FontResolver.GetAscent(font);
        float descent = FontResolver.GetDescent(font);
        float boxHeight = MathF.Max(style.ResolveLineHeight(), ascent + descent);

        // Half-leading centres the glyph box inside the line box, which is what browsers do.
        float halfLeading = MathF.Max(0f, (boxHeight - (ascent + descent)) / 2f);
        float itemAscent = halfLeading + ascent;
        float itemDescent = halfLeading + descent;

        foreach (string token in _lineBreaker.Tokenize(text, style.WhiteSpace))
        {
            if (token == LineBreaker.NewLineToken)
            {
                items.Add(new InlineItem { IsForcedBreak = true });
                continue;
            }

            bool isSpace = token == LineBreaker.SpaceToken;
            items.Add(new InlineItem
            {
                Text = token,
                Font = font,
                Color = style.Color,
                Underline = style.TextDecorationUnderline,
                IsSpace = isSpace,
                Width = _lineBreaker.Measure(token, font),
                BoxHeight = boxHeight,
                Ascent = itemAscent,
                BelowBaseline = itemDescent,
            });
        }
    }

    /// <summary>
    /// Creates an inline item for a replaced element, or falls back to its alternative text.
    /// </summary>
    /// <param name="element">Image element.</param>
    /// <param name="style">Computed style of the element.</param>
    /// <param name="percentBase">Base used to resolve percentage sizes.</param>
    /// <param name="items">List that receives the produced items.</param>
    private void AppendImage(HtmlElement element, ComputedStyle style, float percentBase, List<InlineItem> items)
    {
        if (ImageSizing.TryGetSize(_context, element, style, percentBase, out float width, out float height))
        {
            items.Add(new InlineItem
            {
                Image = _context.GetImage(element.GetAttribute("src")),
                Width = width,
                BoxHeight = height,
                Ascent = height,
                BelowBaseline = 0f,
            });

            return;
        }

        // Missing or unloadable image: show the alternative text instead of a hole.
        string? alt = element.GetAttribute("alt");
        if (!string.IsNullOrEmpty(alt))
        {
            AppendText(alt, style, items);
        }
    }

    /// <summary>
    /// Wraps items into line boxes, honouring wrapping, white-space and hard breaks.
    /// </summary>
    /// <param name="items">Flattened inline items.</param>
    /// <param name="availableWidth">Width available for each line.</param>
    /// <param name="containerStyle">Style of the containing block.</param>
    /// <returns>The produced lines in vertical order.</returns>
    private List<TextLine> BuildLines(List<InlineItem> items, float availableWidth, ComputedStyle containerStyle)
    {
        var lines = new List<TextLine>();
        var line = new TextLine();
        float x = 0f;
        float maxAscent = 0f;
        float maxBelow = 0f;
        float maxBox = 0f;
        bool hasContent = false;
        bool lastIsSpace = false;
        bool sawNonSpace = false;
        bool wrapAllowed = containerStyle.WhiteSpace is WhiteSpaceKind.Normal or WhiteSpaceKind.PreWrap;

        void StartLine()
        {
            line = new TextLine();
            x = 0f;
            maxAscent = 0f;
            maxBelow = 0f;
            maxBox = 0f;
            hasContent = false;
            lastIsSpace = false;
        }

        void FinishLine()
        {
            if (!hasContent)
            {
                // Empty lines (consecutive <br>, empty <p>) still occupy one line box.
                SKFont font = _context.Fonts.GetFont(containerStyle);
                float ascent = FontResolver.GetAscent(font);
                float descent = FontResolver.GetDescent(font);
                float boxHeight = MathF.Max(containerStyle.ResolveLineHeight(), ascent + descent);
                float halfLeading = MathF.Max(0f, (boxHeight - (ascent + descent)) / 2f);
                maxAscent = halfLeading + ascent;
                maxBelow = halfLeading + descent;
                maxBox = boxHeight;
            }

            float height = MathF.Max(maxBox, maxAscent + MathF.Max(0f, maxBelow));
            line.Baseline = maxAscent;
            line.Height = height > 0f ? height : containerStyle.ResolveLineHeight();
            line.Width = x;

            foreach (TextFragment fragment in line.Fragments)
            {
                fragment.Baseline = maxAscent;
            }

            lines.Add(line);
        }

        foreach (InlineItem item in items)
        {
            if (item.IsForcedBreak)
            {
                sawNonSpace = true;
                FinishLine();
                StartLine();
                continue;
            }

            if (item.IsSpace)
            {
                // Leading spaces of a line are dropped, and a trailing space is trimmed on wrap.
                if (!hasContent)
                {
                    continue;
                }

                line.Fragments.Add(CreateFragment(item, x));
                x += item.Width;
                lastIsSpace = true;
                maxAscent = MathF.Max(maxAscent, item.Ascent);
                maxBelow = MathF.Max(maxBelow, item.BelowBaseline);
                maxBox = MathF.Max(maxBox, item.BoxHeight);
                continue;
            }

            if (hasContent && wrapAllowed && !LineBreaker.Fits(x, item.Width, availableWidth))
            {
                if (lastIsSpace && line.Fragments.Count > 0)
                {
                    x -= line.Fragments[^1].Width;
                    line.Fragments.RemoveAt(line.Fragments.Count - 1);
                }

                FinishLine();
                StartLine();
            }

            sawNonSpace = true;
            line.Fragments.Add(CreateFragment(item, x));
            x += item.Width;
            hasContent = true;
            lastIsSpace = false;
            maxAscent = MathF.Max(maxAscent, item.Ascent);
            maxBelow = MathF.Max(maxBelow, item.BelowBaseline);
            maxBox = MathF.Max(maxBox, item.BoxHeight);
        }

        // A group that only held white space produces no line box at all.
        if (hasContent || (lines.Count == 0 && sawNonSpace))
        {
            FinishLine();
        }

        return lines;
    }

    /// <summary>
    /// Creates a fragment positioned at the given horizontal offset inside the current line.
    /// </summary>
    /// <param name="item">Item to convert.</param>
    /// <param name="x">Offset of the fragment inside the line.</param>
    /// <returns>The positioned fragment.</returns>
    private static TextFragment CreateFragment(InlineItem item, float x) => new()
    {
        Text = item.Text,
        Image = item.Image,
        Font = item.Font,
        Color = item.Color,
        Underline = item.Underline,
        Width = item.Width,
        Height = item.BoxHeight,
        X = x,
    };

    /// <summary>
    /// Computes the line offset produced by <c>text-align</c>.
    /// </summary>
    /// <param name="line">Line to align.</param>
    /// <param name="availableWidth">Width of the containing content box.</param>
    /// <param name="align">Requested alignment.</param>
    /// <returns>The horizontal offset of the line.</returns>
    private static float AlignLine(TextLine line, float availableWidth, TextAlign align) => align switch
    {
        TextAlign.Center => MathF.Max(0f, (availableWidth - line.Width) / 2f),
        TextAlign.Right => MathF.Max(0f, availableWidth - line.Width),
        _ => 0f,
    };
}
