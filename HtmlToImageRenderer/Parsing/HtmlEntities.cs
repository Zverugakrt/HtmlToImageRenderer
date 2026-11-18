using System.Text;

namespace HtmlToImageRenderer.Parsing;

/// <summary>
/// Decodes HTML character references (<c>&amp;amp;</c>, <c>&amp;#160;</c>, <c>&amp;#xA0;</c>)
/// in text runs and attribute values. Unknown references are left untouched so that
/// malformed markup does not lose characters.
/// </summary>
public static class HtmlEntities
{
    /// <summary>
    /// Longest reference that is still considered a candidate; anything longer is treated as plain text.
    /// </summary>
    private const int MaxEntityLength = 32;

    private static readonly Dictionary<string, string> NamedEntities = new(StringComparer.Ordinal)
    {
        ["amp"] = "&",
        ["lt"] = "<",
        ["gt"] = ">",
        ["quot"] = "\"",
        ["apos"] = "'",
        ["nbsp"] = "\u00A0",
        ["copy"] = "\u00A9",
        ["reg"] = "\u00AE",
        ["trade"] = "\u2122",
        ["hellip"] = "\u2026",
        ["mdash"] = "\u2014",
        ["ndash"] = "\u2013",
        ["lsquo"] = "\u2018",
        ["rsquo"] = "\u2019",
        ["ldquo"] = "\u201C",
        ["rdquo"] = "\u201D",
        ["laquo"] = "\u00AB",
        ["raquo"] = "\u00BB",
        ["bull"] = "\u2022",
        ["middot"] = "\u00B7",
        ["dagger"] = "\u2020",
        ["Dagger"] = "\u2021",
        ["sect"] = "\u00A7",
        ["para"] = "\u00B6",
        ["deg"] = "\u00B0",
        ["plusmn"] = "\u00B1",
        ["times"] = "\u00D7",
        ["divide"] = "\u00F7",
        ["frac12"] = "\u00BD",
        ["frac14"] = "\u00BC",
        ["frac34"] = "\u00BE",
        ["sup1"] = "\u00B9",
        ["sup2"] = "\u00B2",
        ["sup3"] = "\u00B3",
        ["micro"] = "\u00B5",
        ["euro"] = "\u20AC",
        ["pound"] = "\u00A3",
        ["yen"] = "\u00A5",
        ["cent"] = "\u00A2",
        ["curren"] = "\u00A4",
        ["larr"] = "\u2190",
        ["uarr"] = "\u2191",
        ["rarr"] = "\u2192",
        ["darr"] = "\u2193",
        ["harr"] = "\u2194",
        ["le"] = "\u2264",
        ["ge"] = "\u2265",
        ["ne"] = "\u2260",
        ["asymp"] = "\u2248",
        ["infin"] = "\u221E",
        ["sum"] = "\u2211",
        ["prod"] = "\u220F",
        ["radic"] = "\u221A",
        ["int"] = "\u222B",
        ["part"] = "\u2202",
        ["alpha"] = "\u03B1",
        ["beta"] = "\u03B2",
        ["gamma"] = "\u03B3",
        ["delta"] = "\u03B4",
        ["epsilon"] = "\u03B5",
        ["lambda"] = "\u03BB",
        ["mu"] = "\u03BC",
        ["pi"] = "\u03C0",
        ["sigma"] = "\u03C3",
        ["omega"] = "\u03C9",
        ["Omega"] = "\u03A9",
        ["Delta"] = "\u0394",
        ["hearts"] = "\u2665",
        ["diams"] = "\u2666",
        ["clubs"] = "\u2663",
        ["spades"] = "\u2660",
        ["check"] = "\u2713",
        ["cross"] = "\u2717",
        ["star"] = "\u2606",
        ["starf"] = "\u2605",
        ["shy"] = "\u00AD",
        ["ensp"] = "\u2002",
        ["emsp"] = "\u2003",
        ["thinsp"] = "\u2009",
        ["zwnj"] = "\u200C",
        ["zwj"] = "\u200D",
    };

    /// <summary>
    /// Replaces all character references in <paramref name="input"/> with the characters they denote.
    /// </summary>
    /// <param name="input">Raw markup text or attribute value.</param>
    /// <returns>The decoded string, or the original instance when nothing was decoded.</returns>
    public static string Decode(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input ?? string.Empty;
        }

        int ampersand = input.IndexOf('&');
        if (ampersand < 0)
        {
            return input;
        }

        var builder = new StringBuilder(input.Length);
        int index = 0;

        while (ampersand >= 0 && ampersand < input.Length)
        {
            builder.Append(input, index, ampersand - index);

            int semicolon = input.IndexOf(';', ampersand + 1);
            if (semicolon < 0 || semicolon - ampersand > MaxEntityLength)
            {
                // Not a reference we understand: emit the ampersand and keep scanning.
                builder.Append('&');
                index = ampersand + 1;
            }
            else
            {
                string name = input.Substring(ampersand + 1, semicolon - ampersand - 1);
                if (TryDecodeReference(name, out string? decoded))
                {
                    builder.Append(decoded);
                    index = semicolon + 1;
                }
                else
                {
                    builder.Append('&');
                    index = ampersand + 1;
                }
            }

            ampersand = index < input.Length ? input.IndexOf('&', index) : -1;
        }

        builder.Append(input, index, input.Length - index);
        return builder.ToString();
    }

    /// <summary>
    /// Resolves a single reference body, either <c>#123</c>/<c>#x1F600</c> or a named entity.
    /// </summary>
    /// <param name="name">Reference body without the leading ampersand and trailing semicolon.</param>
    /// <param name="decoded">Decoded characters when the reference is known.</param>
    /// <returns><c>true</c> when the reference was recognized.</returns>
    private static bool TryDecodeReference(string name, out string? decoded)
    {
        if (name.Length > 1 && name[0] == '#')
        {
            bool hex = name[1] is 'x' or 'X';
            string digits = hex ? name[2..] : name[1..];

            if (digits.Length > 0)
            {
                bool valid = hex
                    ? int.TryParse(digits, System.Globalization.NumberStyles.HexNumber, null, out int code)
                    : int.TryParse(digits, System.Globalization.NumberStyles.Integer, null, out code);

                if (valid && code > 0 && code <= 0x10FFFF && (code < 0xD800 || code > 0xDFFF))
                {
                    decoded = char.ConvertFromUtf32(code);
                    return true;
                }
            }
        }
        else if (NamedEntities.TryGetValue(name, out string? named))
        {
            decoded = named;
            return true;
        }

        decoded = null;
        return false;
    }
}
