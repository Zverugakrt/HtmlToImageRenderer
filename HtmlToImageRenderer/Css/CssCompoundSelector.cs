using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Css;

/// <summary>
/// One compound selector such as <c>div.card.big#main</c>: an optional element name
/// combined with an optional id and any number of class names.
/// </summary>
public sealed class CssCompoundSelector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssCompoundSelector"/> class.
    /// </summary>
    /// <param name="tagName">Required element name, or <c>null</c> for any element.</param>
    /// <param name="id">Required id, or <c>null</c>.</param>
    /// <param name="classes">Required class names; may be empty.</param>
    public CssCompoundSelector(string? tagName, string? id, IReadOnlyList<string> classes)
    {
        TagName = tagName;
        Id = id;
        Classes = classes;
    }

    /// <summary>
    /// Gets the required element name, or <c>null</c> when the compound matches any element.
    /// </summary>
    public string? TagName { get; }

    /// <summary>
    /// Gets the required identifier, or <c>null</c>.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Gets the class names the element must all carry.
    /// </summary>
    public IReadOnlyList<string> Classes { get; }

    /// <summary>
    /// Parses a compound selector. Pseudo-classes (<c>:</c>) and attribute selectors
    /// (<c>[</c>) are not supported and are ignored, so <c>a:hover</c> matches like <c>a</c>.
    /// </summary>
    /// <param name="text">Selector fragment without combinators or white space.</param>
    /// <returns>The parsed compound selector.</returns>
    public static CssCompoundSelector Parse(string text)
    {
        string? tagName = null;
        string? id = null;
        var classes = new List<string>();
        int index = 0;

        while (index < text.Length)
        {
            char current = text[index];

            if (current is ':' or '[')
            {
                break;
            }

            if (current == '*')
            {
                index++;
                continue;
            }

            if (current is '#' or '.')
            {
                index++;
                int start = index;
                while (index < text.Length && IsIdentifierChar(text[index]))
                {
                    index++;
                }

                if (index > start)
                {
                    string name = text[start..index];
                    if (current == '#')
                    {
                        id = name;
                    }
                    else
                    {
                        classes.Add(name);
                    }
                }
                else
                {
                    index++;
                }

                continue;
            }

            if (IsIdentifierChar(current))
            {
                int start = index;
                while (index < text.Length && IsIdentifierChar(text[index]))
                {
                    index++;
                }

                tagName = text[start..index].ToLowerInvariant();
                continue;
            }

            index++;
        }

        return new CssCompoundSelector(tagName, id, classes);
    }

    /// <summary>
    /// Determines whether a single element satisfies this compound selector.
    /// </summary>
    /// <param name="element">Element to test.</param>
    /// <returns><c>true</c> when name, id and all classes match.</returns>
    public bool Matches(HtmlElement element)
    {
        if (TagName is not null && !string.Equals(element.TagName, TagName, StringComparison.Ordinal))
        {
            return false;
        }

        if (Id is not null && !string.Equals(element.Id, Id, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string className in Classes)
        {
            if (!element.HasClass(className))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the specificity contributed by this compound selector.
    /// </summary>
    /// <returns>Id, class and type counts for this compound.</returns>
    public CssSpecificity GetSpecificity() => new(
        Id is null ? 0 : 1,
        Classes.Count,
        TagName is null ? 0 : 1);

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(TagName ?? "*");

        if (Id is not null)
        {
            builder.Append('#').Append(Id);
        }

        foreach (string className in Classes)
        {
            builder.Append('.').Append(className);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Determines whether a character can appear in a tag, id or class name.
    /// </summary>
    /// <param name="value">Candidate character.</param>
    /// <returns><c>true</c> for letters, digits, hyphens, underscores and non-ASCII characters.</returns>
    private static bool IsIdentifierChar(char value) =>
        char.IsLetterOrDigit(value) || value is '-' or '_' || value > 127;
}
