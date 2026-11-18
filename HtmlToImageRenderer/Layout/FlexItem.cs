using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Book-keeping for one child of a flex container: the element or anonymous inline group it
/// represents, its resolved style and the main-axis sizes computed by the flex algorithm.
/// </summary>
internal sealed class FlexItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlexItem"/> class for an element child.
    /// </summary>
    /// <param name="element">Element child of the flex container.</param>
    /// <param name="style">Resolved style of the element.</param>
    public FlexItem(HtmlElement element, ComputedStyle style)
    {
        Element = element;
        Style = style;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlexItem"/> class for an anonymous inline group.
    /// </summary>
    /// <param name="group">Group holding the inline content.</param>
    /// <param name="containerStyle">Style of the container, which the inline content inherits.</param>
    public FlexItem(InlineGroup group, ComputedStyle containerStyle)
    {
        Group = group;
        Style = containerStyle;
    }

    /// <summary>
    /// Gets the element of the item, or <c>null</c> when the item wraps anonymous inline content.
    /// </summary>
    public HtmlElement? Element { get; }

    /// <summary>
    /// Gets the anonymous inline group of the item, or <c>null</c> for element items.
    /// </summary>
    public InlineGroup? Group { get; }

    /// <summary>
    /// Gets the style that drives the item and its flex properties.
    /// </summary>
    public ComputedStyle Style { get; }

    /// <summary>
    /// Gets a value indicating whether the item wraps inline content instead of an element.
    /// </summary>
    public bool IsAnonymous => Element is null;

    /// <summary>
    /// Gets or sets the laid out box of the item, which is filled in once the sizes are known.
    /// </summary>
    public LayoutBox? Box { get; set; }

    /// <summary>
    /// Gets or sets the flex base size along the main axis, margins included.
    /// </summary>
    public float BaseSize { get; set; }

    /// <summary>
    /// Gets or sets the final main-axis size after grow or shrink, margins included.
    /// </summary>
    public float TargetSize { get; set; }
}
