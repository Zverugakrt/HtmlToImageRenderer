namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// Builds an <see cref="HtmlDocument"/> tree from the token stream. The parser follows a
/// practical subset of the HTML5 tree construction rules: void elements never get children,
/// stray end tags are ignored, and a few implied end tags (<c>p</c>, <c>li</c>) are honoured
/// so that sloppy markup still produces the shape authors expect.
/// </summary>
public sealed class HtmlParser
{
    /// <summary>
    /// Elements that never have content and therefore must not be pushed on the open-element stack.
    /// </summary>
    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area",
        "base",
        "br",
        "col",
        "embed",
        "hr",
        "img",
        "input",
        "link",
        "meta",
        "param",
        "source",
        "track",
        "wbr",
    };

    /// <summary>
    /// Block-level start tags that implicitly close an open <c>&lt;p&gt;</c>.
    /// </summary>
    private static readonly HashSet<string> ClosesParagraph = new(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "blockquote", "div", "dl", "fieldset", "figure",
        "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hr", "main",
        "nav", "ol", "p", "pre", "section", "table", "ul",
    };

    /// <summary>
    /// Determines whether the given tag is a void element.
    /// </summary>
    /// <param name="tagName">Tag name to test, case insensitive.</param>
    /// <returns><c>true</c> when the element cannot have children.</returns>
    public static bool IsVoidElement(string tagName) => VoidElements.Contains(tagName);

    /// <summary>
    /// Parses markup into a document tree.
    /// </summary>
    /// <param name="html">Markup to parse; <c>null</c> is treated as an empty document.</param>
    /// <returns>The parsed document, owned by the caller.</returns>
    public HtmlDocument Parse(string? html)
    {
        string source = html ?? string.Empty;
        var root = new HtmlElement("#document");
        var document = new HtmlDocument(root, source);
        var tokens = new HtmlTokenizer().Tokenize(source);
        var openElements = new Stack<HtmlElement>();
        openElements.Push(root);

        foreach (HtmlToken token in tokens)
        {
            switch (token.Type)
            {
                case HtmlTokenType.StartTag:
                {
                    if (ClosesParagraph.Contains(token.TagName))
                    {
                        CloseOpenElement(openElements, "p");
                    }

                    if (string.Equals(token.TagName, "li", StringComparison.Ordinal))
                    {
                        CloseOpenElement(openElements, "li");
                    }

                    var element = new HtmlElement(token.TagName);
                    element.Attributes.AddRange(token.Attributes);

                    HtmlElement parent = openElements.Peek();
                    parent.AppendChild(element);

                    if (!token.IsSelfClosing)
                    {
                        openElements.Push(element);
                    }

                    break;
                }

                case HtmlTokenType.EndTag:
                {
                    // Search for the matching open element; mismatch means a stray end tag.
                    if (openElements.Any(e => string.Equals(e.TagName, token.TagName, StringComparison.Ordinal)))
                    {
                        while (openElements.Count > 1)
                        {
                            HtmlElement closed = openElements.Pop();
                            if (string.Equals(closed.TagName, token.TagName, StringComparison.Ordinal))
                            {
                                break;
                            }
                        }
                    }

                    break;
                }

                case HtmlTokenType.Text:
                {
                    if (token.Text.Length == 0)
                    {
                        break;
                    }

                    openElements.Peek().AppendChild(new HtmlTextNode(token.Text));
                    break;
                }
            }
        }

        return document;
    }

    /// <summary>
    /// Pops the innermost open element when it matches the supplied tag name.
    /// Only the top of the stack is considered, which mirrors the "in body" insertion mode
    /// closely enough for rendering purposes.
    /// </summary>
    /// <param name="openElements">Current open-element stack.</param>
    /// <param name="tagName">Tag that should be closed.</param>
    private static void CloseOpenElement(Stack<HtmlElement> openElements, string tagName)
    {
        if (openElements.Count > 1 &&
            string.Equals(openElements.Peek().TagName, tagName, StringComparison.Ordinal))
        {
            openElements.Pop();
        }
    }
}
