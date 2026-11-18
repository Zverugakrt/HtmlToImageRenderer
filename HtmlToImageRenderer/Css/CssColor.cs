using System.Globalization;
using SkiaSharp;

namespace HtmlToImageRenderer.Css;

/// <summary>
/// An RGBA color produced by the cascade. Supports hexadecimal notation,
/// <c>rgb()</c>/<c>rgba()</c>, the <c>transparent</c> keyword and CSS color names.
/// Alpha and color functions with percentages are accepted.
/// </summary>
public readonly struct CssColor : IEquatable<CssColor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssColor"/> struct.
    /// </summary>
    /// <param name="r">Red channel, 0-255.</param>
    /// <param name="g">Green channel, 0-255.</param>
    /// <param name="b">Blue channel, 0-255.</param>
    /// <param name="a">Alpha channel, 0 (transparent) to 255 (opaque).</param>
    public CssColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Gets the red channel, 0-255.
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green channel, 0-255.
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue channel, 0-255.
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Gets the alpha channel, 0 (transparent) to 255 (opaque).
    /// </summary>
    public byte A { get; }

    /// <summary>
    /// Gets the fully transparent color.
    /// </summary>
    public static CssColor Transparent => new(0, 0, 0, 0);

    /// <summary>
    /// Gets opaque black, the initial value of <c>color</c>.
    /// </summary>
    public static CssColor Black => new(0, 0, 0);

    /// <summary>
    /// Gets opaque white.
    /// </summary>
    public static CssColor White => new(255, 255, 255);

    /// <summary>
    /// Converts the color to the SkiaSharp representation.
    /// </summary>
    /// <returns>An <see cref="SKColor"/> with the same channels.</returns>
    public SKColor ToSKColor() => new(R, G, B, A);

    /// <summary>
    /// Creates a CSS color from a SkiaSharp color.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <returns>The converted color.</returns>
    public static CssColor FromSKColor(SKColor color) => new(color.Red, color.Green, color.Blue, color.Alpha);

    /// <summary>
    /// Returns a copy of the color with a different alpha channel.
    /// </summary>
    /// <param name="alpha">New alpha channel, 0-255.</param>
    /// <returns>The recolored value.</returns>
    public CssColor WithAlpha(byte alpha) => new(R, G, B, alpha);

    /// <summary>
    /// Parses a CSS color value.
    /// </summary>
    /// <param name="text">Value such as <c>#ff0000</c>, <c>rgb(0 128 255 / 50%)</c> or <c>tomato</c>.</param>
    /// <param name="color">Parsed color when the method succeeds.</param>
    /// <returns><c>true</c> when the value is a valid color.</returns>
    public static bool TryParse(string? text, out CssColor color)
    {
        color = Transparent;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string value = text.Trim();

        if (value.StartsWith('#'))
        {
            return TryParseHex(value[1..], out color);
        }

        int openParen = value.IndexOf('(');
        if (openParen > 0 && value.EndsWith(')'))
        {
            string function = value[..openParen].Trim().ToLowerInvariant();
            string arguments = value[(openParen + 1)..^1];
            if (function is "rgb" or "rgba" or "hsl" or "hsla")
            {
                // Only the rgb/rgba channel form is decoded; hsl degrades to transparent.
                return function.StartsWith("rgb", StringComparison.Ordinal) &&
                       TryParseRgbFunction(arguments, out color);
            }

            return false;
        }

        string keyword = value.ToLowerInvariant();
        if (keyword == "transparent")
        {
            color = Transparent;
            return true;
        }

        if (NamedColors.TryGet(keyword, out color))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(CssColor other) => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CssColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    /// <inheritdoc />
    public override string ToString() => A == 255
        ? $"#{R:X2}{G:X2}{B:X2}"
        : $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    /// <summary>
    /// Compares two colors for equality.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><c>true</c> when all channels match.</returns>
    public static bool operator ==(CssColor left, CssColor right) => left.Equals(right);

    /// <summary>
    /// Compares two colors for inequality.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><c>true</c> when any channel differs.</returns>
    public static bool operator !=(CssColor left, CssColor right) => !left.Equals(right);

    /// <summary>
    /// Decodes the hexadecimal body of a color, with or without a leading <c>#</c>.
    /// Supports the 3, 4, 6 and 8 digit forms.
    /// </summary>
    /// <param name="hex">Hexadecimal digits.</param>
    /// <param name="color">Decoded color on success.</param>
    /// <returns><c>true</c> when the body is a valid hexadecimal color.</returns>
    private static bool TryParseHex(string hex, out CssColor color)
    {
        color = Transparent;

        if (hex.Length is not (3 or 4 or 6 or 8))
        {
            return false;
        }

        // Short forms are expanded to their full width first, so only two layouts remain.
        string expanded = hex.Length is 3 or 4 ? ExpandShortHex(hex) : hex;

        if (!uint.TryParse(expanded, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed))
        {
            return false;
        }

        color = expanded.Length == 6
            ? new CssColor((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed)
            : new CssColor((byte)(packed >> 24), (byte)(packed >> 16), (byte)(packed >> 8), (byte)packed);

        return true;
    }

    /// <summary>
    /// Turns <c>f0a</c> into <c>ff00aa</c> so a single parser handles both forms.
    /// </summary>
    /// <param name="shortHex">3 or 4 digit hexadecimal body.</param>
    /// <returns>The expanded 6 or 8 digit body.</returns>
    private static string ExpandShortHex(string shortHex)
    {
        var builder = new System.Text.StringBuilder(shortHex.Length * 2);
        foreach (char digit in shortHex)
        {
            builder.Append(digit).Append(digit);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Parses the argument list of <c>rgb()</c> / <c>rgba()</c>, accepting comma or
    /// space separated channels and an optional <c>/ alpha</c> part.
    /// </summary>
    /// <param name="arguments">Text between the parentheses.</param>
    /// <param name="color">Decoded color on success.</param>
    /// <returns><c>true</c> when three or four channels were decoded.</returns>
    private static bool TryParseRgbFunction(string arguments, out CssColor color)
    {
        color = Transparent;

        string normalized = arguments.Replace('/', ' ');
        string[] parts = normalized.Split(
            [',', ' ', '\t', '\n', '\r'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length is not (3 or 4))
        {
            return false;
        }

        if (!TryParseChannel(parts[0], out byte r) ||
            !TryParseChannel(parts[1], out byte g) ||
            !TryParseChannel(parts[2], out byte b))
        {
            return false;
        }

        byte a = 255;
        if (parts.Length == 4 && !TryParseAlpha(parts[3], out a))
        {
            return false;
        }

        color = new CssColor(r, g, b, a);
        return true;
    }

    /// <summary>
    /// Parses a single 0-255 channel expressed as a number or a percentage.
    /// </summary>
    /// <param name="text">Channel text.</param>
    /// <param name="value">Decoded channel.</param>
    /// <returns><c>true</c> when the channel is valid.</returns>
    private static bool TryParseChannel(string text, out byte value)
    {
        value = 0;

        if (text.EndsWith('%'))
        {
            if (!float.TryParse(text[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
            {
                return false;
            }

            value = (byte)Math.Clamp(percent * 2.55f, 0f, 255f);
            return true;
        }

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
        {
            return false;
        }

        value = (byte)Math.Clamp(number, 0f, 255f);
        return true;
    }

    /// <summary>
    /// Parses an alpha value expressed as a number (0-1) or a percentage.
    /// </summary>
    /// <param name="text">Alpha text.</param>
    /// <param name="value">Decoded alpha channel.</param>
    /// <returns><c>true</c> when the alpha is valid.</returns>
    private static bool TryParseAlpha(string text, out byte value)
    {
        value = 255;

        if (text.EndsWith('%'))
        {
            if (!float.TryParse(text[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
            {
                return false;
            }

            value = (byte)Math.Clamp(percent * 2.55f, 0f, 255f);
            return true;
        }

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
        {
            return false;
        }

        value = (byte)Math.Clamp(number * 255f, 0f, 255f);
        return true;
    }
}
