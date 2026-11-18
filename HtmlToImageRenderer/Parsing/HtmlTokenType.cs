namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// Kind of lexical unit produced by <see cref="HtmlTokenizer"/>.
/// </summary>
public enum HtmlTokenType
{
    /// <summary>An opening tag such as <c>&lt;div class="a"&gt;</c>.</summary>
    StartTag,

    /// <summary>A closing tag such as <c>&lt;/div&gt;</c>.</summary>
    EndTag,

    /// <summary>Character data between tags.</summary>
    Text,

    /// <summary>A comment, doctype or any other markup the parser ignores.</summary>
    Comment,

    /// <summary>Sentinel emitted once the input has been fully consumed.</summary>
    EndOfFile,
}
