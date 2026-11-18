using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;
using HtmlToImageRenderer.Resources;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// A laid out box. Coordinates are relative to the content origin of the parent box, so the
/// painter can walk the tree while accumulating offsets. <see cref="Width"/> and
/// <see cref="Height"/> describe the border box.
/// </summary>
public sealed class LayoutBox
{
    /// <summary>
    /// Gets the element this box was generated for, or <c>null</c> for anonymous boxes that
    /// hold the inline content of a block container.
    /// </summary>
    public HtmlElement? Element { get; init; }

    /// <summary>
    /// Gets the computed style driving this box.
    /// </summary>
    public ComputedStyle Style { get; init; } = new();

    /// <summary>
    /// Gets or sets the resolved margin edges.
    /// </summary>
    public BoxEdges Margin { get; set; }

    /// <summary>
    /// Gets or sets the resolved padding edges.
    /// </summary>
    public BoxEdges Padding { get; set; }

    /// <summary>
    /// Gets or sets the resolved border edges.
    /// </summary>
    public BoxEdges Border { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset of the border box relative to the parent content origin.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset of the border box relative to the parent content origin.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the border-box width in pixels.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets or sets the border-box height in pixels.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Gets the child boxes in painting order.
    /// </summary>
    public List<LayoutBox> Children { get; } = new();

    /// <summary>
    /// Gets the text lines of this box when it contains inline content.
    /// </summary>
    public List<TextLine> Lines { get; } = new();

    /// <summary>
    /// Gets or sets the image painted into the content box, used for replaced elements.
    /// </summary>
    public HtmlImage? Image { get; set; }

    /// <summary>
    /// Gets a value indicating whether this box was generated for inline content
    /// rather than for an element.
    /// </summary>
    public bool IsAnonymous => Element is null;

    /// <summary>
    /// Gets the horizontal offset of the content box relative to the parent content origin.
    /// </summary>
    public float ContentX => X + Border.Left + Padding.Left;

    /// <summary>
    /// Gets the vertical offset of the content box relative to the parent content origin.
    /// </summary>
    public float ContentY => Y + Border.Top + Padding.Top;

    /// <summary>
    /// Gets the usable content width in pixels.
    /// </summary>
    public float ContentWidth => MathF.Max(0f, Width - Border.Horizontal - Padding.Horizontal);

    /// <summary>
    /// Gets the usable content height in pixels.
    /// </summary>
    public float ContentHeight => MathF.Max(0f, Height - Border.Vertical - Padding.Vertical);

    /// <summary>
    /// Gets the vertical space this box consumes in a block flow, margins included.
    /// </summary>
    public float OuterHeight => Margin.Vertical + Height;

    /// <inheritdoc />
    public override string ToString() =>
        $"{(IsAnonymous ? "#anonymous" : Element!.TagName)} {X:0.#},{Y:0.#} {Width:0.#}x{Height:0.#}";
}
