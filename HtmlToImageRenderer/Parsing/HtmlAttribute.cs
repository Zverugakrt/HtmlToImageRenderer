namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// A single <c>name="value"</c> pair attached to an element.
/// Attribute names are normalized to lower case, values are entity-decoded.
/// </summary>
public sealed class HtmlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlAttribute"/> class.
    /// </summary>
    /// <param name="name">Lower-case attribute name.</param>
    /// <param name="value">Decoded attribute value, or <c>null</c> for valueless attributes.</param>
    public HtmlAttribute(string name, string? value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the lower-case attribute name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the decoded attribute value. Valueless attributes
    /// (for example <c>disabled</c>) report an empty string.
    /// </summary>
    public string? Value { get; set; }
}
