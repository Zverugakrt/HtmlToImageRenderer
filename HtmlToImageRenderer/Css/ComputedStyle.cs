namespace HtmlToImageRenderer.Css;

/// <summary>
/// The result of the cascade for one element: every property layout and painting need,
/// already inherited where applicable. Lengths that depend on the containing block keep
/// their unit and are resolved by the layout engines.
/// </summary>
public sealed class ComputedStyle
{
    /// <summary>
    /// Gets or sets the generated box type.
    /// </summary>
    public DisplayMode Display { get; set; } = DisplayMode.Inline;

    /// <summary>
    /// Gets or sets the content width, or <c>auto</c> to fill the containing block.
    /// </summary>
    public CssLength Width { get; set; } = CssLength.Auto;

    /// <summary>
    /// Gets or sets the content height, or <c>auto</c> to derive it from content.
    /// </summary>
    public CssLength Height { get; set; } = CssLength.Auto;

    /// <summary>
    /// Gets or sets the minimum content width.
    /// </summary>
    public CssLength MinWidth { get; set; } = CssLength.Auto;

    /// <summary>
    /// Gets or sets the minimum content height.
    /// </summary>
    public CssLength MinHeight { get; set; } = CssLength.Auto;

    /// <summary>
    /// Gets or sets the maximum content width.
    /// </summary>
    public CssLength MaxWidth { get; set; } = CssLength.Auto;

    /// <summary>
    /// Gets or sets the maximum content height.
    /// </summary>
    public CssLength MaxHeight { get; set; } = CssLength.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether the box should be sized to its max-content width,
    /// clamped to the available space, instead of filling the containing block. It is set by the
    /// legacy <c>align</c> attribute of tables, because a table that fills its container cannot be
    /// moved by auto margins.
    /// </summary>
    public bool ShrinkToFitWidth { get; set; }

    /// <summary>Gets or sets the top margin.</summary>
    public CssLength MarginTop { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the right margin.</summary>
    public CssLength MarginRight { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the bottom margin.</summary>
    public CssLength MarginBottom { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the left margin.</summary>
    public CssLength MarginLeft { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the top padding.</summary>
    public CssLength PaddingTop { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the right padding.</summary>
    public CssLength PaddingRight { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the bottom padding.</summary>
    public CssLength PaddingBottom { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the left padding.</summary>
    public CssLength PaddingLeft { get; set; } = CssLength.Zero;

    /// <summary>Gets or sets the top border width in pixels.</summary>
    public float BorderTopWidth { get; set; }

    /// <summary>Gets or sets the right border width in pixels.</summary>
    public float BorderRightWidth { get; set; }

    /// <summary>Gets or sets the bottom border width in pixels.</summary>
    public float BorderBottomWidth { get; set; }

    /// <summary>Gets or sets the left border width in pixels.</summary>
    public float BorderLeftWidth { get; set; }

    /// <summary>
    /// Gets or sets the line style of the top border.
    /// </summary>
    public BorderStyleKind BorderTopStyle { get; set; } = BorderStyleKind.None;

    /// <summary>
    /// Gets or sets the line style of the right border.
    /// </summary>
    public BorderStyleKind BorderRightStyle { get; set; } = BorderStyleKind.None;

    /// <summary>
    /// Gets or sets the line style of the bottom border.
    /// </summary>
    public BorderStyleKind BorderBottomStyle { get; set; } = BorderStyleKind.None;

    /// <summary>
    /// Gets or sets the line style of the left border.
    /// </summary>
    public BorderStyleKind BorderLeftStyle { get; set; } = BorderStyleKind.None;

    /// <summary>
    /// Gets or sets the color of the top border.
    /// </summary>
    public CssColor BorderTopColor { get; set; } = CssColor.Black;

    /// <summary>
    /// Gets or sets the color of the right border.
    /// </summary>
    public CssColor BorderRightColor { get; set; } = CssColor.Black;

    /// <summary>
    /// Gets or sets the color of the bottom border.
    /// </summary>
    public CssColor BorderBottomColor { get; set; } = CssColor.Black;

    /// <summary>
    /// Gets or sets the color of the left border.
    /// </summary>
    public CssColor BorderLeftColor { get; set; } = CssColor.Black;

    /// <summary>
    /// Gets or sets the corner radii. A radius affects painting only; it never changes layout,
    /// which matches CSS.
    /// </summary>
    public CssBorderRadius BorderRadius { get; set; } = CssBorderRadius.None;

    /// <summary>
    /// Gets or sets a value indicating whether declared widths and heights include
    /// padding and borders (<c>box-sizing: border-box</c>).
    /// </summary>
    public bool UseBorderBox { get; set; }

    /// <summary>
    /// Gets or sets the background color painted behind the content and padding box.
    /// </summary>
    public CssColor BackgroundColor { get; set; } = CssColor.Transparent;

    /// <summary>
    /// Gets or sets the text color, which is an inherited property.
    /// </summary>
    public CssColor Color { get; set; } = CssColor.Black;

    /// <summary>
    /// Gets or sets the resolved font family name (the first entry of <c>font-family</c>).
    /// </summary>
    public string FontFamily { get; set; } = "sans-serif";

    /// <summary>
    /// Gets or sets the absolute font size in CSS pixels.
    /// </summary>
    public float FontSize { get; set; } = 16f;

    /// <summary>
    /// Gets or sets the numeric font weight, 100-900.
    /// </summary>
    public int FontWeight { get; set; } = 400;

    /// <summary>
    /// Gets or sets the font slant.
    /// </summary>
    public FontStyleKind FontStyle { get; set; } = FontStyleKind.Normal;

    /// <summary>
    /// Gets or sets the line height multiplier used when <see cref="LineHeightPx"/> is <c>null</c>.
    /// </summary>
    public float LineHeightMultiplier { get; set; } = 1.2f;

    /// <summary>
    /// Gets or sets an absolute line height in pixels, or <c>null</c> to use
    /// <see cref="LineHeightMultiplier"/> against the current font size.
    /// </summary>
    public float? LineHeightPx { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of inline content.
    /// </summary>
    public TextAlign TextAlign { get; set; } = TextAlign.Left;

    /// <summary>
    /// Gets or sets the white-space handling for inline content.
    /// </summary>
    public WhiteSpaceKind WhiteSpace { get; set; } = WhiteSpaceKind.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether text is drawn with an underline.
    /// </summary>
    public bool TextDecorationUnderline { get; set; }

    /// <summary>
    /// Gets or sets the main axis of a flex container.
    /// </summary>
    public FlexDirection FlexDirection { get; set; } = FlexDirection.Row;

    /// <summary>
    /// Gets or sets the wrapping mode of a flex container. Only single-line layout is implemented.
    /// </summary>
    public FlexWrap FlexWrap { get; set; } = FlexWrap.NoWrap;

    /// <summary>
    /// Gets or sets the distribution of free space along the main axis.
    /// </summary>
    public JustifyContent JustifyContent { get; set; } = JustifyContent.FlexStart;

    /// <summary>
    /// Gets or sets the default cross-axis alignment of flex items.
    /// </summary>
    public AlignItems AlignItems { get; set; } = AlignItems.Stretch;

    /// <summary>
    /// Gets or sets the per-item cross-axis alignment override, or <c>null</c> to use
    /// the container's <see cref="AlignItems"/>.
    /// </summary>
    public AlignItems? AlignSelf { get; set; }

    /// <summary>
    /// Gets or sets the vertical gap between flex items, in pixels.
    /// </summary>
    public float RowGap { get; set; }

    /// <summary>
    /// Gets or sets the horizontal gap between flex items, in pixels.
    /// </summary>
    public float ColumnGap { get; set; }

    /// <summary>
    /// Gets or sets the flex grow factor, which distributes positive free space.
    /// </summary>
    public float FlexGrow { get; set; }

    /// <summary>
    /// Gets or sets the flex shrink factor, which distributes negative free space.
    /// </summary>
    public float FlexShrink { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the initial main size of a flex item, or <c>auto</c>.
    /// </summary>
    public CssLength FlexBasis { get; set; } = CssLength.Auto;

    /// <summary>
    /// Gets a value indicating whether the element participates in inline flow.
    /// </summary>
    public bool IsInlineLevel => Display is DisplayMode.Inline or DisplayMode.InlineBlock;

    /// <summary>
    /// Gets the painted top border width: zero when the top side has no style.
    /// </summary>
    public float ResolvedBorderTop => BorderTopStyle == BorderStyleKind.None ? 0f : BorderTopWidth;

    /// <summary>
    /// Gets the painted right border width: zero when the right side has no style.
    /// </summary>
    public float ResolvedBorderRight => BorderRightStyle == BorderStyleKind.None ? 0f : BorderRightWidth;

    /// <summary>
    /// Gets the painted bottom border width: zero when the bottom side has no style.
    /// </summary>
    public float ResolvedBorderBottom => BorderBottomStyle == BorderStyleKind.None ? 0f : BorderBottomWidth;

    /// <summary>
    /// Gets the painted left border width: zero when the left side has no style.
    /// </summary>
    public float ResolvedBorderLeft => BorderLeftStyle == BorderStyleKind.None ? 0f : BorderLeftWidth;

    /// <summary>
    /// Gets a value indicating whether any border side will actually be painted.
    /// </summary>
    public bool HasVisibleBorder =>
        ResolvedBorderTop > 0f || ResolvedBorderRight > 0f || ResolvedBorderBottom > 0f || ResolvedBorderLeft > 0f;

    /// <summary>
    /// Gets the sum of the painted left and right border widths.
    /// </summary>
    public float HorizontalBorderWidth => ResolvedBorderLeft + ResolvedBorderRight;

    /// <summary>
    /// Gets the sum of the painted top and bottom border widths.
    /// </summary>
    public float VerticalBorderWidth => ResolvedBorderTop + ResolvedBorderBottom;

    /// <summary>
    /// Computes the line height in pixels for the current font size.
    /// </summary>
    /// <returns>An absolute line height, never smaller than 90% of the font size.</returns>
    public float ResolveLineHeight()
    {
        float resolved = LineHeightPx ?? (LineHeightMultiplier * FontSize);
        return MathF.Max(resolved, FontSize * 0.9f);
    }

    /// <summary>
    /// Creates a shallow copy, which is how inherited styles are seeded for child elements.
    /// </summary>
    /// <returns>An independent copy of the current values.</returns>
    public ComputedStyle Clone() => (ComputedStyle)MemberwiseClone();
}
