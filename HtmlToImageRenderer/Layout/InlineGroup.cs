using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// A unit of a block container's content: either one block-level child element, or an
/// anonymous run of inline-level nodes that has to be laid out as a single line box group.
/// </summary>
internal sealed class InlineGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InlineGroup"/> class for a block-level element.
    /// </summary>
    /// <param name="element">Block-level child element.</param>
    public InlineGroup(HtmlElement element)
    {
        Element = element;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineGroup"/> class for anonymous inline content.
    /// </summary>
    public InlineGroup()
    {
    }

    /// <summary>
    /// Gets the block-level element of this group, or <c>null</c> when the group holds inline content.
    /// </summary>
    public HtmlElement? Element { get; }

    /// <summary>
    /// Gets the inline-level nodes of an anonymous group, in document order.
    /// </summary>
    public List<HtmlNode> Nodes { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the group holds inline content rather than a block box.
    /// </summary>
    public bool IsAnonymous => Element is null;

    /// <summary>
    /// Holds the style of a block-level group's element once it has been resolved.
    /// </summary>
    public ComputedStyle? Style { get; set; }
}
