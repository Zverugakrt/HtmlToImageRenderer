using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Layout;
using HtmlToImageRenderer.Resources;

namespace HtmlToImageRenderer.Rendering;

/// <summary>
/// Holds everything that survives between renders of one <see cref="HtmlRenderer"/>: the font
/// resolver, the image cache and the parsed user-agent stylesheet. Instances are owned by a single
/// renderer, so options changed on that renderer invalidate the affected part of the cache.
/// </summary>
internal sealed class RenderCache : IDisposable
{
    /// <summary>
    /// Parsed user-agent sheet. Its CSS source is a compile-time constant, so one parsed copy is
    /// shared by every renderer instead of being re-parsed on each render; the sheet is only read.
    /// </summary>
    private static readonly Lazy<CssStylesheet> SharedUserAgentSheet =
        new(() => new CssParser().Parse(UserAgentStylesheet.Css), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _gate = new();

    private FontResolver? _fonts;
    private string? _fontFamily;

    private ImageCache? _images;
    private int _imageCapacity;
    private string? _baseUri;
    private bool _loadRemoteImages;
    private TimeSpan _imageTimeout;

    private bool _disposed;

    /// <summary>
    /// Gets the shared parsed user-agent stylesheet.
    /// </summary>
    public CssStylesheet UserAgentSheet => SharedUserAgentSheet.Value;

    /// <summary>
    /// Gets the font cache for the given options, recreating it when the default family changed,
    /// because the resolver bakes that family in.
    /// </summary>
    /// <param name="options">Options currently in effect on the renderer.</param>
    /// <returns>The font resolver owned by this cache.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the cache was disposed.</exception>
    public FontResolver GetFonts(HtmlRendererOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            string family = options.DefaultFontFamily;

            if (_fonts is null || !string.Equals(_fontFamily, family, StringComparison.Ordinal))
            {
                _fonts?.Dispose();
                _fonts = new FontResolver(family);
                _fontFamily = family;
            }

            return _fonts;
        }
    }

    /// <summary>
    /// Gets the image cache for the given options, or <c>null</c> when caching is disabled. The
    /// cache is recreated when its capacity or any option that affects loading changes.
    /// </summary>
    /// <param name="options">Options currently in effect on the renderer.</param>
    /// <returns>The cache, or <c>null</c> when <see cref="HtmlRendererOptions.ImageCacheSize"/> disables it.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the cache was disposed.</exception>
    public ImageCache? GetImages(HtmlRendererOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (options.ImageCacheSize <= 0)
            {
                // Caching disabled: the loader owns and disposes whatever it decodes.
                _images?.Dispose();
                _images = null;
                return null;
            }

            bool configurationChanged =
                _images is null ||
                _imageCapacity != options.ImageCacheSize ||
                !string.Equals(_baseUri, options.BaseUri, StringComparison.Ordinal) ||
                _loadRemoteImages != options.LoadRemoteImages ||
                _imageTimeout != options.ImageLoadTimeout;

            if (configurationChanged)
            {
                // Loading behaviour changed, so previously cached images may no longer be valid.
                _images?.Dispose();
                _images = new ImageCache(options.ImageCacheSize);
                _imageCapacity = options.ImageCacheSize;
                _baseUri = options.BaseUri;
                _loadRemoteImages = options.LoadRemoteImages;
                _imageTimeout = options.ImageLoadTimeout;
            }

            return _images;
        }
    }

    /// <summary>
    /// Releases the font resolver and the image cache.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _fonts?.Dispose();
            _fonts = null;

            _images?.Dispose();
            _images = null;
        }
    }
}
