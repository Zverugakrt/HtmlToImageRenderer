using System.Text;
using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Layout;
using HtmlToImageRenderer.Parsing;
using HtmlToImageRenderer.Resources;
using SkiaSharp;

namespace HtmlToImageRenderer.Rendering;

/// <summary>
/// Runs one render: collects the CSS, resolves styles, loads images, lays the document out and
/// paints it. The pipeline owns the font cache and the decoded images, so it must stay alive
/// until painting is finished and then be disposed.
/// </summary>
internal sealed class RenderPipeline : IDisposable
{
    private readonly HtmlRendererOptions _options;
    private readonly RenderCache _cache;
    private readonly FontResolver _fonts;
    private readonly ImageLoader _images;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderPipeline"/> class.
    /// </summary>
    /// <param name="options">Renderer options for this render.</param>
    /// <param name="cache">Caches shared by all renders of one renderer.</param>
    public RenderPipeline(HtmlRendererOptions options, RenderCache cache)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);

        _options = options;
        _cache = cache;

        // Fonts and images outlive the render; only the per-render loader is created here.
        _fonts = cache.GetFonts(options);
        _images = new ImageLoader(options, cache.GetImages(options));
    }

    /// <summary>
    /// Builds the style sheets, loads images and lays out the document.
    /// </summary>
    /// <param name="document">Parsed document.</param>
    /// <param name="cancellationToken">Token that aborts image downloads.</param>
    /// <returns>The root layout box.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is <c>null</c>.</exception>
    public async Task<LayoutBox> BuildLayoutAsync(HtmlDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var parser = new CssParser();

        // The user-agent sheet is the lowest-precedence sheet; author rules override it through
        // the normal cascade, inline styles override both. It is parsed once per process.
        CssStylesheet userAgentSheet = _cache.UserAgentSheet;
        CssStylesheet authorSheet = parser.Parse(CollectStyleText(document));

        await _images.PreloadAsync(document, cancellationToken).ConfigureAwait(false);

        var styles = new StyleResolver(userAgentSheet, authorSheet, _options);
        var context = new LayoutContext(_options, styles, _fonts, _images.Images, _options.Width);
        return new LayoutEngine(context).Layout(document.GetLayoutRoot());
    }

    /// <summary>
    /// Paints a laid out document onto a canvas.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="root">Root box produced by <see cref="BuildLayoutAsync"/>.</param>
    public void Paint(SKCanvas canvas, LayoutBox root)
    {
        // The root font size lets the painter resolve rem based corner radii.
        using var painter = new HtmlPainter(_options.DefaultFontSize);
        painter.Paint(canvas, root);
    }

    /// <summary>
    /// Releases the font cache and the decoded images.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Fonts and decoded images belong to the render cache; the loader only releases this
        // render's references so later renders can keep using the same instances.
        _images.Dispose();
    }

    /// <summary>
    /// Concatenates the content of every <c>&lt;style&gt;</c> element of the document.
    /// </summary>
    /// <param name="document">Document to scan.</param>
    /// <returns>The combined CSS source, or an empty string.</returns>
    private static string CollectStyleText(HtmlDocument document)
    {
        var builder = new StringBuilder();

        foreach (HtmlElement element in document.EnumerateElements())
        {
            if (!string.Equals(element.TagName, "style", StringComparison.Ordinal))
            {
                continue;
            }

            builder.AppendLine(element.GetTextContent());
        }

        return builder.ToString();
    }
}
