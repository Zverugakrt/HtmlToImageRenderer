using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Resources;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Everything the layout engines need for one render: options, resolved styles, the font
/// cache, the decoded images and the viewport width. A context is cheap and lives for a
/// single render only.
/// </summary>
public sealed class LayoutContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutContext"/> class.
    /// </summary>
    /// <param name="options">Renderer options that provide dimensions and default fonts.</param>
    /// <param name="styles">Resolver that produces computed styles.</param>
    /// <param name="fonts">Font cache used for measuring text.</param>
    /// <param name="images">Decoded images keyed by their original <c>src</c> value.</param>
    /// <param name="viewportWidth">Width available for the root box, in CSS pixels.</param>
    public LayoutContext(
        HtmlRendererOptions options,
        StyleResolver styles,
        FontResolver fonts,
        IReadOnlyDictionary<string, HtmlImage> images,
        float viewportWidth)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(styles);
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(images);

        Options = options;
        Styles = styles;
        Fonts = fonts;
        Images = images;
        ViewportWidth = MathF.Max(1f, viewportWidth);
    }

    /// <summary>
    /// Gets the renderer options in effect.
    /// </summary>
    public HtmlRendererOptions Options { get; }

    /// <summary>
    /// Gets the style resolver, which also caches computed styles per element.
    /// </summary>
    public StyleResolver Styles { get; }

    /// <summary>
    /// Gets the font cache.
    /// </summary>
    public FontResolver Fonts { get; }

    /// <summary>
    /// Gets the decoded images keyed by the original <c>src</c> string.
    /// </summary>
    public IReadOnlyDictionary<string, HtmlImage> Images { get; }

    /// <summary>
    /// Gets the width available for layout, in CSS pixels.
    /// </summary>
    public float ViewportWidth { get; }

    /// <summary>
    /// Gets the root font size, which defines <c>1rem</c>.
    /// </summary>
    public float RootFontSize => Options.DefaultFontSize;

    /// <summary>
    /// Resolves a CSS length against a percentage base and a style's font size.
    /// </summary>
    /// <param name="length">Length to resolve.</param>
    /// <param name="percentBase">Value that percentages resolve against.</param>
    /// <param name="style">Style providing the font size for <c>em</c> units.</param>
    /// <returns>The length in CSS pixels.</returns>
    public float ResolveLength(CssLength length, float percentBase, ComputedStyle style) =>
        length.Resolve(percentBase, style.FontSize, RootFontSize);

    /// <summary>
    /// Looks up a decoded image by its original source string.
    /// </summary>
    /// <param name="source">Value of the <c>src</c> attribute.</param>
    /// <returns>The image, or <c>null</c> when it was not loaded.</returns>
    public HtmlImage? GetImage(string? source) =>
        source is not null && Images.TryGetValue(source, out HtmlImage? image) ? image : null;
}
