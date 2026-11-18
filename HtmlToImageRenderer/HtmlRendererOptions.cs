using SkiaSharp;

namespace HtmlToImageRenderer;

/// <summary>
/// Describes how HTML markup is laid out and rasterized into an image.
/// Instances are consumed by <see cref="HtmlRenderer"/> and can be reused across renders.
/// </summary>
public sealed class HtmlRendererOptions
{
    /// <summary>
    /// Gets or sets the logical viewport width, in CSS pixels.
    /// This is the width used for layout (percentage widths, wrapping, centering).
    /// </summary>
    public int Width { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the logical output height, in CSS pixels.
    /// When <c>null</c> (the default) the height is derived from the laid out content.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the upper bound applied to content-derived heights.
    /// Prevents unbounded documents from requesting gigapixel bitmaps.
    /// </summary>
    public int MaxAutoHeight { get; set; } = 20000;

    /// <summary>
    /// Gets or sets the raster scale factor. The output bitmap is
    /// <see cref="Width"/> * <see cref="DeviceScaleFactor"/> pixels wide,
    /// while layout keeps working in logical CSS pixels.
    /// </summary>
    public float DeviceScaleFactor { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the color used to clear the output surface before painting.
    /// Use <c>SKColors.Transparent</c> for a transparent background.
    /// </summary>
    public SKColor BackgroundColor { get; set; } = SKColors.White;

    /// <summary>
    /// Gets or sets the base location used to resolve relative image sources.
    /// May be an absolute file path, a directory path, or an absolute HTTP(S) URL.
    /// When <c>null</c> relative sources cannot be resolved and are skipped.
    /// </summary>
    public string? BaseUri { get; set; }

    /// <summary>
    /// Gets or sets the font family used when neither the markup nor the stylesheets specify one.
    /// </summary>
    public string DefaultFontFamily { get; set; } = "Segoe UI";

    /// <summary>
    /// Gets or sets the root font size, in CSS pixels. It defines the value of <c>1rem</c>
    /// and the base for percentages and <c>em</c> units on the root element.
    /// </summary>
    public float DefaultFontSize { get; set; } = 16f;

    /// <summary>
    /// Gets or sets the line height multiplier used when no <c>line-height</c> is declared.
    /// </summary>
    public float DefaultLineHeight { get; set; } = 1.2f;

    /// <summary>
    /// Gets or sets a value indicating whether images served over HTTP(S) are downloaded.
    /// When <c>false</c>, remote sources are ignored (data URIs and local files keep working),
    /// which is useful for offline or sandboxed renders.
    /// </summary>
    public bool LoadRemoteImages { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum time allowed for a single image download.
    /// </summary>
    public TimeSpan ImageLoadTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the maximum number of decoded images kept in the renderer's cache, keyed by
    /// their <c>src</c> value. When the cache overflows, the least recently used image is dropped.
    /// Set to zero or a negative value to disable image caching, in which case every render loads
    /// and disposes its own images.
    /// </summary>
    public int ImageCacheSize { get; set; } = 64;

    /// <summary>
    /// Gets or sets a value indicating whether the rendered bitmap is cropped to the bounding box
    /// of the non-background pixels. Useful when the content height is not known up front and the
    /// untouched area would otherwise be a solid block of the background color.
    /// </summary>
    public bool TrimBackground { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether PNG output is converted to a two level (black and
    /// white) 8-bit grayscale image. A pixel becomes black when its BT.601 luminance is below 50%,
    /// otherwise white. <see cref="HtmlRenderer.RenderToBitmap"/> is unaffected because a 1-bit
    /// raster cannot be represented by <c>SKBitmap</c>.
    /// </summary>
    public bool Monochrome { get; set; }

    /// <summary>
    /// Creates a deep copy of the current options, so callers can tweak a render
    /// without mutating the instance held by an <see cref="HtmlRenderer"/>.
    /// </summary>
    /// <returns>A new <see cref="HtmlRendererOptions"/> with identical values.</returns>
    public HtmlRendererOptions Clone() => new()
    {
        Width = Width,
        Height = Height,
        MaxAutoHeight = MaxAutoHeight,
        DeviceScaleFactor = DeviceScaleFactor,
        BackgroundColor = BackgroundColor,
        BaseUri = BaseUri,
        DefaultFontFamily = DefaultFontFamily,
        DefaultFontSize = DefaultFontSize,
        DefaultLineHeight = DefaultLineHeight,
        LoadRemoteImages = LoadRemoteImages,
        ImageLoadTimeout = ImageLoadTimeout,
        ImageCacheSize = ImageCacheSize,
        TrimBackground = TrimBackground,
        Monochrome = Monochrome,
    };
}
