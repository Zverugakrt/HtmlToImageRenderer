namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// Root of a parsed markup tree. The tree is anchored by a synthetic
/// <c>#document</c> element whose children are whatever the source contained,
/// which keeps parsing tolerant of fragments without <c>&lt;html&gt;</c>.
/// </summary>
public sealed class HtmlDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlDocument"/> class and
    /// assigns itself as the owner of every node in the subtree.
    /// </summary>
    /// <param name="root">Synthetic document root.</param>
    /// <param name="sourceHtml">Markup the document was parsed from.</param>
    public HtmlDocument(HtmlElement root, string sourceHtml)
    {
        Root = root;
        SourceHtml = sourceHtml;
        AssignOwner(root, this);
    }

    /// <summary>
    /// Gets the synthetic <c>#document</c> root element.
    /// </summary>
    public HtmlElement Root { get; }

    /// <summary>
    /// Gets the markup this document was parsed from.
    /// </summary>
    public string SourceHtml { get; }

    /// <summary>
    /// Gets the first <c>&lt;body&gt;</c> element, or <c>null</c> when the markup has none.
    /// </summary>
    public HtmlElement? Body => FindFirst("body");

    /// <summary>
    /// Gets the first <c>&lt;head&gt;</c> element, or <c>null</c> when the markup has none.
    /// </summary>
    public HtmlElement? Head => FindFirst("head");

    /// <summary>
    /// Returns the element that layout should start from: <c>&lt;body&gt;</c> when present,
    /// otherwise the first <c>&lt;html&gt;</c> element, otherwise the synthetic root.
    /// </summary>
    /// <returns>The layout root element.</returns>
    public HtmlElement GetLayoutRoot() => Body ?? FindFirst("html") ?? Root;

    /// <summary>
    /// Enumerates every element of the document in depth-first document order.
    /// </summary>
    /// <returns>A lazy sequence of elements, starting with <see cref="Root"/>.</returns>
    public IEnumerable<HtmlElement> EnumerateElements()
    {
        foreach (var element in EnumerateElements(Root))
        {
            yield return element;
        }
    }

    /// <summary>
    /// Returns the first element with the given tag name.
    /// </summary>
    /// <param name="tagName">Lower-case tag name to search for.</param>
    /// <returns>The first matching element, or <c>null</c>.</returns>
    public HtmlElement? FindFirst(string tagName)
    {
        foreach (var element in EnumerateElements())
        {
            if (string.Equals(element.TagName, tagName, StringComparison.Ordinal))
            {
                return element;
            }
        }

        return null;
    }

    private static IEnumerable<HtmlElement> EnumerateElements(HtmlElement element)
    {
        yield return element;

        foreach (var child in element.Children)
        {
            if (child is HtmlElement nested)
            {
                foreach (var descendant in EnumerateElements(nested))
                {
                    yield return descendant;
                }
            }
        }
    }

    private static void AssignOwner(HtmlElement element, HtmlDocument document)
    {
        element.OwnerDocument = document;

        foreach (var child in element.Children)
        {
            child.OwnerDocument = document;
            if (child is HtmlElement nested)
            {
                AssignOwner(nested, document);
            }
        }
    }
}
