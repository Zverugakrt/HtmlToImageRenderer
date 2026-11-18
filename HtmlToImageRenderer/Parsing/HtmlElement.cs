namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// An element node of the parsed tree. Tag and attribute names are normalized to
/// lower case, children keep their source order.
/// </summary>
public sealed class HtmlElement : HtmlNode
{
    private readonly List<HtmlNode> _children = new();
    private IReadOnlyList<string>? _classes;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlElement"/> class.
    /// </summary>
    /// <param name="tagName">Tag name; it is stored lower-cased.</param>
    public HtmlElement(string tagName)
    {
        TagName = tagName.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the lower-case tag name.
    /// </summary>
    public string TagName { get; }

    /// <summary>
    /// Gets the attributes declared on the element, in source order.
    /// </summary>
    public List<HtmlAttribute> Attributes { get; } = new();

    /// <summary>
    /// Gets the child nodes in document order.
    /// </summary>
    public IReadOnlyList<HtmlNode> Children => _children;

    /// <summary>
    /// Gets the value of the <c>id</c> attribute, or <c>null</c> when absent.
    /// </summary>
    public string? Id => GetAttribute("id");

    /// <summary>
    /// Gets the class names from the <c>class</c> attribute, split on white space.
    /// </summary>
    public IReadOnlyList<string> Classes => _classes ??= ParseClasses();

    /// <inheritdoc />
    public override string NodeName => TagName;

    /// <summary>
    /// Appends a child node and wires up its parent link.
    /// </summary>
    /// <param name="child">Node to append; must not be <c>null</c>.</param>
    public void AppendChild(HtmlNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        child.Parent = this;
        _children.Add(child);
    }

    /// <summary>
    /// Looks up an attribute value by its lower-case name.
    /// </summary>
    /// <param name="name">Lower-case attribute name.</param>
    /// <returns>The attribute value, or <c>null</c> when the attribute is not present.</returns>
    public string? GetAttribute(string name)
    {
        foreach (var attribute in Attributes)
        {
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return attribute.Value ?? string.Empty;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the element declares the given attribute.
    /// </summary>
    /// <param name="name">Lower-case attribute name.</param>
    /// <returns><c>true</c> when the attribute exists.</returns>
    public bool HasAttribute(string name) => GetAttribute(name) is not null;

    /// <summary>
    /// Determines whether the element carries the given CSS class.
    /// </summary>
    /// <param name="className">Class name to test for, case sensitive.</param>
    /// <returns><c>true</c> when the class is present.</returns>
    public bool HasClass(string className)
    {
        foreach (var name in Classes)
        {
            if (string.Equals(name, className, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Concatenates the text of all descendant text nodes, which is how the content
    /// of <c>&lt;style&gt;</c> and <c>&lt;script&gt;</c> elements is read.
    /// </summary>
    /// <returns>The concatenated character data; empty when the element has no text children.</returns>
    public string GetTextContent()
    {
        var builder = new System.Text.StringBuilder();
        CollectText(this, builder);
        return builder.ToString();
    }

    private static void CollectText(HtmlElement element, System.Text.StringBuilder builder)
    {
        foreach (var child in element._children)
        {
            switch (child)
            {
                case HtmlTextNode text:
                    builder.Append(text.Text);
                    break;
                case HtmlElement nested:
                    CollectText(nested, builder);
                    break;
            }
        }
    }

    private IReadOnlyList<string> ParseClasses()
    {
        var raw = GetAttribute("class");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <inheritdoc />
    public override string ToString() => $"<{TagName}>";
}
