namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Whitespace classification used by the layout engines. .NET puts the no-break space (U+00A0) and
/// the narrow no-break space (U+202F) into the Unicode "separator, space" category, so
/// <see cref="char.IsWhiteSpace(char)"/> reports them as ordinary spaces. Layout must not collapse
/// them and must not use them as line-break opportunities, hence these helpers.
/// </summary>
internal static class WhitespaceRules
{
    /// <summary>No-break space.</summary>
    public const char NoBreakSpace = '\u00A0';

    /// <summary>Narrow no-break space.</summary>
    public const char NarrowNoBreakSpace = '\u202F';

    /// <summary>
    /// Determines whether a character is whitespace that may be collapsed and broken at.
    /// </summary>
    /// <param name="character">Character to test.</param>
    /// <returns><c>true</c> for collapsible whitespace; <c>false</c> for whitespace that binds words.</returns>
    public static bool IsCollapsible(char character) =>
        char.IsWhiteSpace(character) &&
        character != NoBreakSpace &&
        character != NarrowNoBreakSpace;

    /// <summary>
    /// Determines whether a string is empty or consists only of collapsible whitespace.
    /// </summary>
    /// <param name="value">Text to test; <c>null</c> counts as empty.</param>
    /// <returns><c>true</c> when the text carries no visible content.</returns>
    public static bool IsNullOrCollapsible(string? value)
    {
        if (value is null)
        {
            return true;
        }

        foreach (char character in value)
        {
            if (!IsCollapsible(character))
            {
                return false;
            }
        }

        return true;
    }
}
