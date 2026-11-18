namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// Splits markup into a flat token stream. The tokenizer is intentionally tolerant:
/// malformed constructs degrade into text or are skipped, and it always terminates with
/// an <see cref="HtmlTokenType.EndOfFile"/> token so the parser needs no lookahead tricks.
/// </summary>
public sealed class HtmlTokenizer
{
    /// <summary>
    /// Elements whose content is not parsed as markup, so CSS or JavaScript containing
    /// <c>&lt;</c> survives intact.
    /// </summary>
    private static readonly HashSet<string> RawTextElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "style",
        "script",
        "textarea",
        "title",
    };

    /// <summary>
    /// Tokenizes the supplied markup.
    /// </summary>
    /// <param name="html">Markup to tokenize; <c>null</c> is treated as an empty document.</param>
    /// <returns>The tokens in source order, terminated by an <see cref="HtmlTokenType.EndOfFile"/> token.</returns>
    public IReadOnlyList<HtmlToken> Tokenize(string? html)
    {
        var tokens = new List<HtmlToken>();
        string source = html ?? string.Empty;
        int index = 0;
        string? rawTextTag = null;

        while (index < source.Length)
        {
            if (rawTextTag is not null)
            {
                int closing = source.IndexOf("</" + rawTextTag, index, StringComparison.OrdinalIgnoreCase);
                string rawText;

                if (closing < 0)
                {
                    rawText = source[index..];
                    index = source.Length;
                }
                else
                {
                    rawText = source[index..closing];
                    index = closing;
                }

                if (rawText.Length > 0)
                {
                    tokens.Add(new HtmlToken
                    {
                        Type = HtmlTokenType.Text,
                        Text = HtmlEntities.Decode(rawText),
                        Position = closing < 0 ? index - rawText.Length : closing - rawText.Length,
                    });
                }

                rawTextTag = null;
                continue;
            }

            char current = source[index];

            if (current != '<')
            {
                int next = source.IndexOf('<', index);
                if (next < 0)
                {
                    next = source.Length;
                }

                string text = source[index..next];
                if (text.Length > 0)
                {
                    tokens.Add(new HtmlToken
                    {
                        Type = HtmlTokenType.Text,
                        Text = HtmlEntities.Decode(text),
                        Position = index,
                    });
                }

                index = next;
                continue;
            }

            if (StartsWith(source, index, "<!--"))
            {
                int end = source.IndexOf("-->", index + 4, StringComparison.Ordinal);
                index = end < 0 ? source.Length : end + 3;
                tokens.Add(new HtmlToken { Type = HtmlTokenType.Comment, Position = index });
                continue;
            }

            if (StartsWith(source, index, "<!"))
            {
                int end = source.IndexOf('>', index + 2);
                index = end < 0 ? source.Length : end + 1;
                tokens.Add(new HtmlToken { Type = HtmlTokenType.Comment, Position = index });
                continue;
            }

            if (StartsWith(source, index, "</"))
            {
                int end = source.IndexOf('>', index + 2);
                if (end < 0)
                {
                    // Unterminated end tag: treat the remainder as text so nothing is lost.
                    tokens.Add(new HtmlToken
                    {
                        Type = HtmlTokenType.Text,
                        Text = source[index..],
                        Position = index,
                    });
                    index = source.Length;
                    continue;
                }

                string rawName = source[(index + 2)..end].Trim();
                int nameEnd = rawName.IndexOfAny([' ', '\t', '\r', '\n', '/']);
                if (nameEnd >= 0)
                {
                    rawName = rawName[..nameEnd];
                }

                if (rawName.Length > 0)
                {
                    tokens.Add(new HtmlToken
                    {
                        Type = HtmlTokenType.EndTag,
                        TagName = rawName.ToLowerInvariant(),
                        Position = index,
                    });
                }

                index = end + 1;
                continue;
            }

            if (index + 1 < source.Length && IsTagNameStart(source[index + 1]))
            {
                HtmlToken token = ReadStartTag(source, index, out int nextIndex);
                tokens.Add(token);
                index = nextIndex;

                if (!token.IsSelfClosing && RawTextElements.Contains(token.TagName))
                {
                    rawTextTag = token.TagName;
                }

                continue;
            }

            // A lone '<' that does not open markup, for example "1 < 2".
            tokens.Add(new HtmlToken { Type = HtmlTokenType.Text, Text = "<", Position = index });
            index++;
        }

        tokens.Add(new HtmlToken { Type = HtmlTokenType.EndOfFile, Position = source.Length });
        return tokens;
    }

    /// <summary>
    /// Reads a complete start tag including its attributes.
    /// </summary>
    /// <param name="source">Markup being tokenized.</param>
    /// <param name="start">Offset of the opening angle bracket.</param>
    /// <param name="nextIndex">Offset immediately after the closing angle bracket.</param>
    /// <returns>The parsed start-tag token.</returns>
    private static HtmlToken ReadStartTag(string source, int start, out int nextIndex)
    {
        int position = start + 1;
        int nameStart = position;

        while (position < source.Length && IsTagNameChar(source[position]))
        {
            position++;
        }

        string tagName = source[nameStart..position].ToLowerInvariant();
        var attributes = new List<HtmlAttribute>();
        bool selfClosing = false;

        while (position < source.Length)
        {
            while (position < source.Length && char.IsWhiteSpace(source[position]))
            {
                position++;
            }

            if (position >= source.Length)
            {
                break;
            }

            if (source[position] == '>')
            {
                position++;
                break;
            }

            if (source[position] == '/')
            {
                if (position + 1 < source.Length && source[position + 1] == '>')
                {
                    selfClosing = true;
                    position += 2;
                    break;
                }

                position++;
                continue;
            }

            int attributeStart = position;
            while (position < source.Length &&
                   !char.IsWhiteSpace(source[position]) &&
                   source[position] is not ('=' or '>' or '/'))
            {
                position++;
            }

            string attributeName = source[attributeStart..position].ToLowerInvariant();
            string? attributeValue = string.Empty;

            while (position < source.Length && char.IsWhiteSpace(source[position]))
            {
                position++;
            }

            if (position < source.Length && source[position] == '=')
            {
                position++;
                while (position < source.Length && char.IsWhiteSpace(source[position]))
                {
                    position++;
                }

                if (position < source.Length && source[position] is '"' or '\'')
                {
                    char quote = source[position++];
                    int valueStart = position;
                    while (position < source.Length && source[position] != quote)
                    {
                        position++;
                    }

                    attributeValue = HtmlEntities.Decode(source[valueStart..position]);

                    if (position < source.Length)
                    {
                        position++;
                    }
                }
                else
                {
                    int valueStart = position;
                    while (position < source.Length &&
                           !char.IsWhiteSpace(source[position]) &&
                           source[position] != '>')
                    {
                        position++;
                    }

                    attributeValue = HtmlEntities.Decode(source[valueStart..position]);
                }
            }

            if (attributeName.Length > 0)
            {
                attributes.Add(new HtmlAttribute(attributeName, attributeValue));
            }
            else if (attributeStart == position)
            {
                // Guard against a non-advancing loop on unparsable input.
                position++;
            }
        }

        nextIndex = Math.Min(position, source.Length);
        return new HtmlToken
        {
            Type = HtmlTokenType.StartTag,
            TagName = tagName,
            Attributes = attributes,
            IsSelfClosing = selfClosing || HtmlParser.IsVoidElement(tagName),
            Position = start,
        };
    }

    /// <summary>
    /// Determines whether a character may begin a tag name.
    /// </summary>
    /// <param name="value">Candidate character.</param>
    /// <returns><c>true</c> for letters.</returns>
    private static bool IsTagNameStart(char value) => char.IsLetter(value);

    /// <summary>
    /// Determines whether a character may continue a tag name.
    /// </summary>
    /// <param name="value">Candidate character.</param>
    /// <returns><c>true</c> for letters, digits, hyphens, underscores and colons.</returns>
    private static bool IsTagNameChar(char value) =>
        char.IsLetterOrDigit(value) || value is '-' or '_' or ':';

    /// <summary>
    /// Checks for a prefix at a given offset without allocating a substring.
    /// </summary>
    /// <param name="source">Text to inspect.</param>
    /// <param name="offset">Offset to start at.</param>
    /// <param name="prefix">Expected prefix.</param>
    /// <returns><c>true</c> when the prefix is present at the offset.</returns>
    private static bool StartsWith(string source, int offset, string prefix) =>
        source.AsSpan(offset).StartsWith(prefix, StringComparison.Ordinal);
}
