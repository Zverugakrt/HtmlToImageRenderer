namespace HtmlToImageRenderer.Css;

/// <summary>
/// Parses CSS source into a <see cref="CssStylesheet"/>. The parser understands rule blocks,
/// comma separated selector lists and declaration blocks; at-rules (including the contents of
/// <c>@media</c>) are skipped as a whole, and malformed declarations are ignored instead of
/// aborting the parse.
/// </summary>
public sealed class CssParser
{
    /// <summary>
    /// Parses a stylesheet.
    /// </summary>
    /// <param name="css">CSS source; <c>null</c> or empty produces an empty stylesheet.</param>
    /// <returns>The parsed stylesheet, never <c>null</c>.</returns>
    public CssStylesheet Parse(string? css)
    {
        var stylesheet = new CssStylesheet();
        if (string.IsNullOrWhiteSpace(css))
        {
            return stylesheet;
        }

        string text = StripComments(css);
        int index = 0;

        while (index < text.Length)
        {
            int braceIndex = text.IndexOf('{', index);
            if (braceIndex < 0)
            {
                break;
            }

            int semicolonIndex = text.IndexOf(';', index);
            if (semicolonIndex >= 0 && semicolonIndex < braceIndex)
            {
                // A block-less at-rule such as @import or @charset: not supported, just skip it.
                index = semicolonIndex + 1;
                continue;
            }

            string prelude = text[index..braceIndex].Trim();
            int blockEnd = FindMatchingBrace(text, braceIndex);
            string body = text[(braceIndex + 1)..blockEnd];
            index = Math.Min(blockEnd + 1, text.Length);

            if (prelude.Length == 0 || prelude[0] == '@')
            {
                continue;
            }

            var selectors = new List<CssSelector>();
            foreach (string part in SplitTopLevel(prelude, ','))
            {
                string selectorText = part.Trim();
                if (selectorText.Length > 0)
                {
                    selectors.Add(CssSelector.Parse(selectorText));
                }
            }

            IReadOnlyList<CssDeclaration> declarations = ParseDeclarations(body);
            if (selectors.Count == 0 || declarations.Count == 0)
            {
                continue;
            }

            stylesheet.Add(new CssRule(selectors, declarations));
        }

        return stylesheet;
    }

    /// <summary>
    /// Parses a declaration block, as found in a rule body or in a <c>style</c> attribute.
    /// </summary>
    /// <param name="block">Text containing <c>property: value</c> pairs separated by semicolons.</param>
    /// <returns>The declarations in source order; unknown or empty declarations are dropped.</returns>
    public IReadOnlyList<CssDeclaration> ParseDeclarations(string? block)
    {
        var declarations = new List<CssDeclaration>();
        if (string.IsNullOrWhiteSpace(block))
        {
            return declarations;
        }

        foreach (string chunk in SplitTopLevel(block, ';'))
        {
            string declaration = chunk.Trim();
            if (declaration.Length == 0)
            {
                continue;
            }

            int colon = IndexOfTopLevel(declaration, ':');
            if (colon <= 0)
            {
                continue;
            }

            string property = declaration[..colon].Trim().ToLowerInvariant();
            string value = declaration[(colon + 1)..].Trim();

            // !important is not part of the cascade model here, but the marker must not
            // leak into the value text: the declaration simply keeps its normal priority.
            int important = value.IndexOf("!important", StringComparison.OrdinalIgnoreCase);
            if (important >= 0)
            {
                value = value[..important].Trim();
            }

            if (property.Length == 0 || value.Length == 0)
            {
                continue;
            }

            declarations.Add(new CssDeclaration(property, value));
        }

        return declarations;
    }

    /// <summary>
    /// Removes <c>/* ... */</c> comments so that selectors and values can be trimmed safely.
    /// </summary>
    /// <param name="css">CSS source possibly containing comments.</param>
    /// <returns>The source without comments.</returns>
    private static string StripComments(string css)
    {
        var builder = new System.Text.StringBuilder(css.Length);
        int index = 0;

        while (index < css.Length)
        {
            int start = css.IndexOf("/*", index, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(css, index, css.Length - index);
                break;
            }

            builder.Append(css, index, start - index);

            int end = css.IndexOf("*/", start + 2, StringComparison.Ordinal);
            index = end < 0 ? css.Length : end + 2;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Finds the closing brace that matches the opening brace at <paramref name="openIndex"/>.
    /// </summary>
    /// <param name="text">Text to scan.</param>
    /// <param name="openIndex">Offset of the opening brace.</param>
    /// <returns>Offset of the matching closing brace, or the end of the text when unbalanced.</returns>
    private static int FindMatchingBrace(string text, int openIndex)
    {
        int depth = 0;
        char quote = '\0';

        for (int index = openIndex; index < text.Length; index++)
        {
            char current = text[index];

            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            switch (current)
            {
                case '"' or '\'':
                    quote = current;
                    break;
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return index;
                    }

                    break;
            }
        }

        return text.Length;
    }

    /// <summary>
    /// Splits text on a separator character that appears outside of quotes and parentheses.
    /// This keeps values such as <c>url(data:image/png;base64,AAA)</c> in one piece.
    /// </summary>
    /// <param name="text">Text to split.</param>
    /// <param name="separator">Separator character.</param>
    /// <returns>The separated pieces, without the separators.</returns>
    private static List<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        int depth = 0;
        char quote = '\0';
        int start = 0;

        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];

            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            switch (current)
            {
                case '"' or '\'':
                    quote = current;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth = Math.Max(0, depth - 1);
                    break;
                default:
                    if (current == separator && depth == 0)
                    {
                        parts.Add(text[start..index]);
                        start = index + 1;
                    }

                    break;
            }
        }

        parts.Add(text[start..]);
        return parts;
    }

    /// <summary>
    /// Finds the first occurrence of a character outside of quotes and parentheses.
    /// </summary>
    /// <param name="text">Text to scan.</param>
    /// <param name="target">Character to look for.</param>
    /// <returns>The offset, or <c>-1</c> when the character does not appear at the top level.</returns>
    private static int IndexOfTopLevel(string text, char target)
    {
        int depth = 0;
        char quote = '\0';

        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];

            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            switch (current)
            {
                case '"' or '\'':
                    quote = current;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth = Math.Max(0, depth - 1);
                    break;
                default:
                    if (current == target && depth == 0)
                    {
                        return index;
                    }

                    break;
            }
        }

        return -1;
    }
}
