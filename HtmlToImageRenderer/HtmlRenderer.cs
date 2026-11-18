using HtmlToImageRenderer.Layout;
using HtmlToImageRenderer.Parsing;
using HtmlToImageRenderer.Rendering;
using SkiaSharp;

namespace HtmlToImageRenderer;

/// <summary>
/// Renders HTML markup to an image. The renderer parses the markup, applies the CSS cascade,
/// lays the document out and rasterizes it with SkiaSharp, returning the result as PNG bytes
/// or as an <see cref="SKBitmap"/> owned by the caller.
/// </summary>
/// <example>
/// <code>
/// var renderer = new HtmlRenderer(new HtmlRendererOptions { Width = 800 });
/// byte[] png = renderer.RenderToPng("&lt;div style=\"display:flex\"&gt;&lt;b&gt;Hello&lt;/b&gt;&lt;/div&gt;");
/// File.WriteAllBytes("page.png", png);
/// </code>
/// </example>
public sealed class HtmlRenderer : IDisposable
{
    private readonly RenderCache _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlRenderer"/> class.
    /// </summary>
    /// <param name="options">Render options; a default instance is used when <c>null</c>.</param>
    public HtmlRenderer(HtmlRendererOptions? options = null)
    {
        Options = options ?? new HtmlRendererOptions();
    }

    /// <summary>
    /// Gets the options this renderer uses. They are read on every render, so changing them
    /// affects subsequent calls.
    /// </summary>
    public HtmlRendererOptions Options { get; }

    /// <summary>
    /// Releases the caches owned by this renderer: the font cache and the decoded image cache.
    /// The renderer must not be used afterwards.
    /// </summary>
    public void Dispose() => _cache.Dispose();

    /// <summary>
    /// Renders markup and returns the image as PNG bytes.
    /// </summary>
    /// <param name="html">Markup to render.</param>
    /// <returns>The PNG encoded image.</returns>
    /// <exception cref="HtmlRenderException">Thrown when <paramref name="html"/> is <c>null</c> or rendering fails.</exception>
    public byte[] RenderToPng(string html) =>
        RenderToPngAsync(html).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Renders markup asynchronously and returns the image as PNG bytes.
    /// </summary>
    /// <param name="html">Markup to render.</param>
    /// <param name="cancellationToken">Token that aborts image downloads and the render.</param>
    /// <returns>The PNG encoded image.</returns>
    /// <exception cref="HtmlRenderException">Thrown when <paramref name="html"/> is <c>null</c> or rendering fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public async Task<byte[]> RenderToPngAsync(string html, CancellationToken cancellationToken = default)
    {
        using SKBitmap bitmap = await RenderToBitmapAsync(html, cancellationToken).ConfigureAwait(false);

        if (!Options.Monochrome)
        {
            return PngEncoder.EncodePng(bitmap);
        }

        // Trimming already ran on the colour bitmap inside Rasterize, so the threshold never
        // influences the crop bounds.
        using SKBitmap monochrome = MonochromeConverter.ToMonochrome(bitmap);
        return PngEncoder.EncodePng(monochrome);
    }

    /// <summary>
    /// Renders markup and returns the rasterized bitmap.
    /// </summary>
    /// <param name="html">Markup to render.</param>
    /// <returns>A bitmap owned by the caller, who must dispose it.</returns>
    /// <exception cref="HtmlRenderException">Thrown when <paramref name="html"/> is <c>null</c> or rendering fails.</exception>
    public SKBitmap RenderToBitmap(string html) =>
        RenderToBitmapAsync(html).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Renders markup asynchronously and returns the rasterized bitmap.
    /// </summary>
    /// <param name="html">Markup to render.</param>
    /// <param name="cancellationToken">Token that aborts image downloads and the render.</param>
    /// <returns>A bitmap owned by the caller, who must dispose it.</returns>
    /// <exception cref="HtmlRenderException">Thrown when <paramref name="html"/> is <c>null</c> or rendering fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public async Task<SKBitmap> RenderToBitmapAsync(string html, CancellationToken cancellationToken = default)
    {
        HtmlRenderException.ThrowIfNullHtml(html);

        try
        {
            HtmlDocument document = new HtmlParser().Parse(html);

            using var pipeline = new RenderPipeline(Options, _cache);
            LayoutBox root = await pipeline.BuildLayoutAsync(document, cancellationToken).ConfigureAwait(false);

            return Rasterize(pipeline, root);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HtmlRenderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new HtmlRenderException("Failed to rasterize the HTML document.", exception);
        }
    }

    /// <summary>
    /// Renders markup directly onto a caller supplied canvas, which allows compositing into an
    /// existing surface.
    /// </summary>
    /// <param name="html">Markup to render.</param>
    /// <param name="canvas">Target canvas; its current transform is respected.</param>
    /// <exception cref="HtmlRenderException">Thrown when <paramref name="html"/> is <c>null</c> or rendering fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="canvas"/> is <c>null</c>.</exception>
    public void Render(string html, SKCanvas canvas) =>
        RenderAsync(html, canvas).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Renders markup asynchronously onto a caller supplied canvas.
    /// </summary>
    /// <param name="html">Markup to render.</param>
    /// <param name="canvas">Target canvas; its current transform is respected.</param>
    /// <param name="cancellationToken">Token that aborts image downloads and the render.</param>
    /// <returns>A task that completes when the canvas has been painted.</returns>
    /// <exception cref="HtmlRenderException">Thrown when <paramref name="html"/> is <c>null</c> or rendering fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="canvas"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public async Task RenderAsync(string html, SKCanvas canvas, CancellationToken cancellationToken = default)
    {
        HtmlRenderException.ThrowIfNullHtml(html);
        ArgumentNullException.ThrowIfNull(canvas);

        try
        {
            HtmlDocument document = new HtmlParser().Parse(html);

            using var pipeline = new RenderPipeline(Options, _cache);
            LayoutBox root = await pipeline.BuildLayoutAsync(document, cancellationToken).ConfigureAwait(false);

            pipeline.Paint(canvas, root);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HtmlRenderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new HtmlRenderException("Failed to render the HTML document.", exception);
        }
    }

    /// <summary>
    /// Allocates the output bitmap and paints the laid out document into it.
    /// </summary>
    /// <param name="pipeline">Pipeline that owns the fonts and images of this render.</param>
    /// <param name="root">Root layout box.</param>
    /// <returns>The rasterized bitmap, owned by the caller.</returns>
    private SKBitmap Rasterize(RenderPipeline pipeline, LayoutBox root)
    {
        float scale = Options.DeviceScaleFactor > 0f ? Options.DeviceScaleFactor : 1f;
        float documentHeight = MathF.Max(LayoutEngine.GetDocumentHeight(root), 1f);

        int logicalWidth = Math.Max(1, Options.Width);
        int logicalHeight = Options.Height.HasValue
            ? Math.Max(1, Options.Height.Value)
            : Math.Max(1, (int)MathF.Ceiling(MathF.Min(documentHeight, Math.Max(1, Options.MaxAutoHeight))));

        // Layout works in CSS pixels; the bitmap is allocated in device pixels.
        int pixelWidth = Math.Max(1, (int)MathF.Ceiling(logicalWidth * scale));
        int pixelHeight = Math.Max(1, (int)MathF.Ceiling(logicalHeight * scale));

        var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);

        try
        {
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(Options.BackgroundColor);

                if (MathF.Abs(scale - 1f) > 0.0001f)
                {
                    canvas.Scale(scale, scale);
                }

                pipeline.Paint(canvas, root);
                canvas.Flush();
            }

            if (!Options.TrimBackground)
            {
                return bitmap;
            }

            // The trimmer hands back the same instance when nothing can be cropped, so the
            // original is disposed only when a different bitmap was produced.
            SKBitmap trimmed = BackgroundTrimmer.Trim(bitmap, Options.BackgroundColor);

            if (!ReferenceEquals(trimmed, bitmap))
            {
                bitmap.Dispose();
            }

            return trimmed;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
