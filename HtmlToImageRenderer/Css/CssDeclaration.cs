namespace HtmlToImageRenderer.Css;

/// <summary>
/// A single <c>property: value</c> pair as it appeared in a rule or an inline style attribute.
/// Property names are lower-cased and <c>!important</c> markers are stripped by the parser.
/// </summary>
public sealed class CssDeclaration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssDeclaration"/> class.
    /// </summary>
    /// <param name="property">Lower-case property name.</param>
    /// <param name="value">Raw, trimmed value text.</param>
    public CssDeclaration(string property, string value)
    {
        Property = property;
        Value = value;
    }

    /// <summary>
    /// Gets the lower-case property name.
    /// </summary>
    public string Property { get; }

    /// <summary>
    /// Gets the raw value text, exactly as authored apart from trimming.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Property}: {Value}";
}
