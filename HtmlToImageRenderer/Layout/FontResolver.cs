using HtmlToImageRenderer.Css;
using SkiaSharp;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Resolves CSS font descriptions to cached <see cref="SKFont"/> instances and measures text
/// with them. Fonts are created once per (family, size, weight, style) combination and live
/// as long as the resolver, which makes measurement during layout cheap.
/// </summary>
public sealed class FontResolver : IDisposable
{
    private static readonly string[] SansSerifFallbacks = ["Segoe UI", "Arial", "Helvetica", "DejaVu Sans", "Liberation Sans"];
    private static readonly string[] SerifFallbacks = ["Times New Roman", "Georgia", "DejaVu Serif", "Liberation Serif"];
    private static readonly string[] MonospaceFallbacks = ["Consolas", "Courier New", "DejaVu Sans Mono", "Liberation Mono"];

    private readonly Dictionary<FontKey, SKFont> _fonts = new();
    private readonly Dictionary<TypefaceKey, SKTypeface> _typefaces = new();
    private readonly HashSet<SKTypeface> _ownedTypefaces = new(ReferenceEqualityComparer.Instance);
    private readonly string _defaultFamily;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontResolver"/> class.
    /// </summary>
    /// <param name="defaultFamily">Family used when a style names none; falls back to <c>sans-serif</c>.</param>
    public FontResolver(string? defaultFamily)
    {
        _defaultFamily = string.IsNullOrWhiteSpace(defaultFamily) ? "sans-serif" : defaultFamily;
    }

    /// <summary>
    /// Gets the font described by a computed style.
    /// </summary>
    /// <param name="style">Style providing family, size, weight and slant.</param>
    /// <returns>A cached font owned by this resolver.</returns>
    public SKFont GetFont(ComputedStyle style) =>
        GetFont(style.FontFamily, style.FontSize, style.FontWeight, style.FontStyle);

    /// <summary>
    /// Gets the font for an explicit description.
    /// </summary>
    /// <param name="family">Family name; generic CSS families are mapped to installed fonts.</param>
    /// <param name="size">Font size in pixels; values below one pixel are clamped.</param>
    /// <param name="weight">Numeric weight, 100-900.</param>
    /// <param name="fontStyle">Slant of the font.</param>
    /// <returns>A cached font owned by this resolver.</returns>
    public SKFont GetFont(string? family, float size, int weight, FontStyleKind fontStyle)
    {
        string resolvedFamily = string.IsNullOrWhiteSpace(family) ? _defaultFamily : family;
        var key = new FontKey(resolvedFamily, MathF.Max(size, 1f), weight, fontStyle);

        if (_fonts.TryGetValue(key, out SKFont? cached))
        {
            return cached;
        }

        SKTypeface typeface = GetTypeface(resolvedFamily, weight, fontStyle);
        var font = new SKFont(typeface, key.Size);
        _fonts[key] = font;
        return font;
    }

    /// <summary>
    /// Measures the advance width of a text run.
    /// </summary>
    /// <param name="text">Text to measure.</param>
    /// <param name="font">Font to measure with.</param>
    /// <returns>The advance width in pixels; zero for empty text.</returns>
    public float MeasureText(string? text, SKFont font) =>
        string.IsNullOrEmpty(text) ? 0f : font.MeasureText(text);

    /// <summary>
    /// Gets the ascent of a font as a positive distance above the baseline.
    /// </summary>
    /// <param name="font">Font to query.</param>
    /// <returns>The ascent in pixels.</returns>
    public static float GetAscent(SKFont font) => MathF.Abs(font.Metrics.Ascent);

    /// <summary>
    /// Gets the descent of a font as a positive distance below the baseline.
    /// </summary>
    /// <param name="font">Font to query.</param>
    /// <returns>The descent in pixels.</returns>
    public static float GetDescent(SKFont font) => MathF.Max(font.Metrics.Descent, 0f);

    /// <summary>
    /// Releases every font and typeface created by this resolver.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (SKFont font in _fonts.Values)
        {
            font.Dispose();
        }

        _fonts.Clear();

        // SKTypeface.Default is process wide: it is cached but never owned by us.
        foreach (SKTypeface typeface in _ownedTypefaces)
        {
            if (!ReferenceEquals(typeface, SKTypeface.Default))
            {
                typeface.Dispose();
            }
        }

        _ownedTypefaces.Clear();
        _typefaces.Clear();
    }

    /// <summary>
    /// Looks up a typeface for a family, trying the CSS generic fallbacks when the named family
    /// is not installed, and finally the platform default.
    /// </summary>
    /// <param name="family">Requested family name.</param>
    /// <param name="weight">Numeric weight.</param>
    /// <param name="fontStyle">Slant of the font.</param>
    /// <returns>A typeface; never <c>null</c>.</returns>
    private SKTypeface GetTypeface(string family, int weight, FontStyleKind fontStyle)
    {
        var key = new TypefaceKey(family, weight, fontStyle);

        if (_typefaces.TryGetValue(key, out SKTypeface? cached))
        {
            return cached;
        }

        var fontStyleValue = new SKFontStyle(
            weight,
            (int)SKFontStyleWidth.Normal,
            fontStyle == FontStyleKind.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        SKTypeface? typeface = SKFontManager.Default.MatchFamily(family, fontStyleValue);

        if (typeface is null)
        {
            foreach (string fallback in GetFallbackFamilies(family))
            {
                typeface = SKFontManager.Default.MatchFamily(fallback, fontStyleValue);
                if (typeface is not null)
                {
                    break;
                }
            }
        }

        typeface ??= SKTypeface.Default;

        if (!ReferenceEquals(typeface, SKTypeface.Default))
        {
            _ownedTypefaces.Add(typeface);
        }

        _typefaces[key] = typeface;
        return typeface;
    }

    /// <summary>
    /// Provides concrete families to try for a generic CSS family keyword.
    /// </summary>
    /// <param name="family">Requested family name, possibly a generic keyword.</param>
    /// <returns>Candidate families in preferred order.</returns>
    private IEnumerable<string> GetFallbackFamilies(string family) => family.ToLowerInvariant() switch
    {
        "serif" => SerifFallbacks,
        "monospace" => MonospaceFallbacks,
        "cursive" => ["Comic Sans MS", "Segoe Script"],
        "fantasy" => ["Impact", "Segoe Print"],
        "sans-serif" or "system-ui" or "ui-sans-serif" => SansSerifFallbacks,
        _ => [_defaultFamily, .. SansSerifFallbacks],
    };

    /// <summary>
    /// Cache key for a resolved font.
    /// </summary>
    /// <param name="Family">Family name after defaulting.</param>
    /// <param name="Size">Size in pixels, clamped away from zero.</param>
    /// <param name="Weight">Numeric weight.</param>
    /// <param name="Style">Slant of the font.</param>
    private readonly record struct FontKey(string Family, float Size, int Weight, FontStyleKind Style);

    /// <summary>
    /// Cache key for a resolved typeface, which is independent of the size.
    /// </summary>
    /// <param name="Family">Requested family name.</param>
    /// <param name="Weight">Numeric weight.</param>
    /// <param name="Style">Slant of the font.</param>
    private readonly record struct TypefaceKey(string Family, int Weight, FontStyleKind Style);
}
