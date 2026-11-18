namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// One lexical unit produced by <see cref="HtmlTokenizer"/>: a tag, a text run,
/// or an ignorable construct such as a comment.
/// </summary>
public sealed class HtmlToken
{
    /// <summary>
    /// Gets the token kind.
    /// </summary>
    public HtmlTokenType Type { get; init; }

    /// <summary>
    /// Gets the lower-case tag name for <see cref="HtmlTokenType.StartTag"/> and
    /// <see cref="HtmlTokenType.EndTag"/> tokens; empty otherwise.
    /// </summary>
    public string TagName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the decoded character data for <see cref="HtmlTokenType.Text"/> tokens.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Gets the attributes of a start tag, in source order.
    /// </summary>
    public IReadOnlyList<HtmlAttribute> Attributes { get; init; } = Array.Empty<HtmlAttribute>();

    /// <summary>
    /// Gets a value indicating whether the start tag was explicitly self closed
    /// (<c>&lt;br/&gt;</c>) or belongs to a void element.
    /// </summary>
    public bool IsSelfClosing { get; init; }

    /// <summary>
    /// Gets the zero-based offset of the token in the source markup, used for diagnostics.
    /// </summary>
    public int Position { get; init; }
}
