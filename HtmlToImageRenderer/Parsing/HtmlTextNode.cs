namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// A character-data node. Entities are already decoded, so the text may contain
/// non-breaking spaces and line breaks exactly as authored.
/// </summary>
public sealed class HtmlTextNode : HtmlNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlTextNode"/> class.
    /// </summary>
    /// <param name="text">Decoded character data.</param>
    public HtmlTextNode(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets the decoded character data held by this node.
    /// </summary>
    public string Text { get; }

    /// <inheritdoc />
    public override string NodeName => "#text";
}
