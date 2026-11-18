using System.Globalization;

namespace HtmlToImageRenderer.Css;

/// <summary>
/// Helpers that turn raw declaration values into typed CSS values. All methods are
/// forgiving: an unparsable value yields the caller supplied fallback so that a single bad
/// declaration never invalidates a whole rule.
/// </summary>
public static class CssValueParser
{
    /// <summary>
    /// Parses a length value such as <c>10px</c>, <c>50%</c>, <c>1.5em</c>, <c>2rem</c>,
    /// <c>12pt</c>, a bare number or the <c>auto</c> keyword.
    /// </summary>
    /// <param name="text">Raw value text.</param>
    /// <param name="fallback">Value returned when parsing fails.</param>
    /// <returns>The parsed length.</returns>
    public static CssLength ParseLength(string? text, CssLength fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        string value = text.Trim().ToLowerInvariant();

        if (value is "auto" or "inherit" or "initial" or "unset")
        {
            return value == "auto" ? CssLength.Auto : fallback;
        }

        if (value.EndsWith('%') &&
            TryParseNumber(value[..^1], out float percent))
        {
            return CssLength.FromPercent(percent);
        }

        if (value.EndsWith("px") && TryParseNumber(value[..^2], out float pixels))
        {
            return new CssLength(pixels, LengthUnit.Px);
        }

        if (value.EndsWith("pt") && TryParseNumber(value[..^2], out float points))
        {
            return new CssLength(points, LengthUnit.Pt);
        }

        if (value.EndsWith("rem") && TryParseNumber(value[..^3], out float rems))
        {
            return new CssLength(rems, LengthUnit.Rem);
        }

        if (value.EndsWith("em") && TryParseNumber(value[..^2], out float ems))
        {
            return new CssLength(ems, LengthUnit.Em);
        }

        // Unitless lengths are only meaningful as zero in CSS; other bare numbers are
        // treated as pixels, which is far more useful for hand written markup.
        if (TryParseNumber(value, out float number))
        {
            return new CssLength(number, LengthUnit.Px);
        }

        return fallback;
    }

    /// <summary>
    /// Parses a font size, resolving relative units against the parent font size and the root size.
    /// </summary>
    /// <param name="text">Raw value text.</param>
    /// <param name="parentFontSize">Computed font size of the parent element.</param>
    /// <param name="rootFontSize">Root font size, used by <c>rem</c> and for keyword sizes.</param>
    /// <param name="fallback">Value returned when parsing fails.</param>
    /// <returns>The absolute font size in CSS pixels.</returns>
    public static float ParseFontSize(string? text, float parentFontSize, float rootFontSize, float fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        string value = text.Trim().ToLowerInvariant();

        switch (value)
        {
            case "xx-small":
                return rootFontSize * 0.5625f;
            case "x-small":
                return rootFontSize * 0.625f;
            case "small":
                return rootFontSize * 0.8125f;
            case "medium":
                return rootFontSize;
            case "large":
                return rootFontSize * 1.125f;
            case "x-large":
                return rootFontSize * 1.5f;
            case "xx-large":
                return rootFontSize * 2f;
            case "smaller":
                return parentFontSize * 0.83f;
            case "larger":
                return parentFontSize * 1.2f;
            case "inherit":
                return parentFontSize;
        }

        if (value.EndsWith('%') && TryParseNumber(value[..^1], out float percent))
        {
            return MathF.Max(parentFontSize * percent / 100f, 1f);
        }

        if (value.EndsWith("rem") && TryParseNumber(value[..^3], out float rems))
        {
            return MathF.Max(rems * rootFontSize, 1f);
        }

        if (value.EndsWith("em") && TryParseNumber(value[..^2], out float ems))
        {
            return MathF.Max(ems * parentFontSize, 1f);
        }

        CssLength length = ParseLength(value, CssLength.Auto);
        return length.IsAuto ? fallback : MathF.Max(length.Resolve(parentFontSize, parentFontSize, rootFontSize), 1f);
    }

    /// <summary>
    /// Expands a one-to-four value shorthand such as <c>margin: 10px 20px</c>.
    /// </summary>
    /// <param name="text">Raw shorthand value.</param>
    /// <param name="top">Top value.</param>
    /// <param name="right">Right value.</param>
    /// <param name="bottom">Bottom value.</param>
    /// <param name="left">Left value.</param>
    /// <returns><c>true</c> when at least one value was parsed.</returns>
    public static bool TryParseShorthand(string? text, out CssLength top, out CssLength right, out CssLength bottom, out CssLength left)
    {
        top = right = bottom = left = CssLength.Auto;

        string[] parts = SplitValues(text);
        if (parts.Length is < 1 or > 4)
        {
            return false;
        }

        var lengths = new CssLength[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            lengths[index] = ParseLength(parts[index], CssLength.Auto);
        }

        top = lengths[0];
        right = lengths.Length > 1 ? lengths[1] : top;
        bottom = lengths.Length > 2 ? lengths[2] : top;
        left = lengths.Length > 3 ? lengths[3] : right;
        return true;
    }

    /// <summary>
    /// Splits a value on white space while keeping quoted strings and functional notation intact.
    /// </summary>
    /// <param name="text">Raw value text.</param>
    /// <returns>The individual tokens; empty when the input is blank.</returns>
    public static string[] SplitValues(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;
        char quote = '\0';

        foreach (char character in text)
        {
            if (quote != '\0')
            {
                current.Append(character);
                if (character == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            switch (character)
            {
                case '"' or '\'':
                    quote = character;
                    current.Append(character);
                    break;
                case '(':
                    depth++;
                    current.Append(character);
                    break;
                case ')':
                    depth = Math.Max(0, depth - 1);
                    current.Append(character);
                    break;
                default:
                    if (char.IsWhiteSpace(character) && depth == 0)
                    {
                        if (current.Length > 0)
                        {
                            tokens.Add(current.ToString());
                            current.Clear();
                        }
                    }
                    else
                    {
                        current.Append(character);
                    }

                    break;
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens.ToArray();
    }

    /// <summary>
    /// Parses a floating point number using the invariant culture.
    /// </summary>
    /// <param name="text">Numeric text.</param>
    /// <param name="value">Parsed value on success.</param>
    /// <returns><c>true</c> when the text is a number.</returns>
    public static bool TryParseNumber(string? text, out float value)
    {
        value = 0f;
        return !string.IsNullOrWhiteSpace(text) &&
               float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Removes surrounding single or double quotes from a value.
    /// </summary>
    /// <param name="text">Quoted or unquoted text.</param>
    /// <returns>The unquoted text.</returns>
    public static string Unquote(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length >= 2 &&
            ((trimmed[0] == '"' && trimmed[^1] == '"') || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }

    /// <summary>
    /// Extracts the first family name from a <c>font-family</c> value, dropping quotes and
    /// any following fallbacks.
    /// </summary>
    /// <param name="text">Raw value such as <c>"Fira Sans", Arial, sans-serif</c>.</param>
    /// <returns>The first family name, or <c>null</c> when the value is empty.</returns>
    public static string? ParseFirstFontFamily(string? text)
    {
        string[] parts = SplitValues(text);
        if (parts.Length == 0)
        {
            return null;
        }

        string family = parts[0];
        int comma = family.IndexOf(',');
        if (comma >= 0)
        {
            family = family[..comma];
        }

        family = Unquote(family).Trim();
        return family.Length == 0 ? null : family;
    }
}
