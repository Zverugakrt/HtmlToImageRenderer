namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// Base type for every node of the parsed document tree.
/// </summary>
public abstract class HtmlNode
{
    /// <summary>
    /// Gets the owning element, or <c>null</c> for the document root.
    /// </summary>
    public HtmlElement? Parent { get; internal set; }

    /// <summary>
    /// Gets the document this node belongs to.
    /// </summary>
    public HtmlDocument? OwnerDocument { get; internal set; }

    /// <summary>
    /// Gets a diagnostic name for the node, such as the tag name or <c>#text</c>.
    /// </summary>
    public abstract string NodeName { get; }
}
