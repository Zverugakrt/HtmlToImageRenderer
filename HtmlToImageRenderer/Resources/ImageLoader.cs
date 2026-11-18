using System.Net.Http;
using HtmlToImageRenderer.Parsing;
using SkiaSharp;

namespace HtmlToImageRenderer.Resources;

/// <summary>
/// Loads and decodes the images referenced by a document. Sources may be Base64 data URIs,
/// HTTP(S) URLs, absolute or <see cref="HtmlRendererOptions.BaseUri"/>-relative file paths.
/// Every source is fetched at most once, and a failure only drops that image.
/// </summary>
public sealed class ImageLoader : IDisposable
{
    private static readonly HttpClient Client = CreateHttpClient();

    private readonly HtmlRendererOptions _options;
    private readonly ImageCache? _cache;
    private readonly Dictionary<string, Task<HtmlImage?>> _pending = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HtmlImage> _images = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageLoader"/> class.
    /// </summary>
    /// <param name="options">Renderer options controlling remote loading, timeouts and the base URI.</param>
    /// <param name="cache">
    /// Cache shared between renders, or <c>null</c> when caching is disabled; in that case this
    /// loader owns and disposes every image it decodes.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    public ImageLoader(HtmlRendererOptions options, ImageCache? cache)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _cache = cache;
    }

    /// <summary>
    /// Gets the successfully decoded images keyed by their original <c>src</c> string.
    /// The dictionary is complete once <see cref="PreloadAsync"/> has finished.
    /// </summary>
    public IReadOnlyDictionary<string, HtmlImage> Images => _images;

    /// <summary>
    /// Loads every image referenced by <c>&lt;img&gt;</c> elements of the document, in parallel.
    /// </summary>
    /// <param name="document">Parsed document whose images should be fetched.</param>
    /// <param name="cancellationToken">Token that aborts the downloads.</param>
    /// <returns>A task that completes when all images have been attempted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is <c>null</c>.</exception>
    public async Task PreloadAsync(HtmlDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var sources = new HashSet<string>(StringComparer.Ordinal);
        foreach (HtmlElement element in document.EnumerateElements())
        {
            if (!string.Equals(element.TagName, "img", StringComparison.Ordinal))
            {
                continue;
            }

            string? source = element.GetAttribute("src");
            if (!string.IsNullOrWhiteSpace(source))
            {
                sources.Add(source);
            }
        }

        if (sources.Count == 0)
        {
            return;
        }

        var loads = new List<Task>(sources.Count);
        foreach (string source in sources)
        {
            loads.Add(LoadAsync(source, cancellationToken));
        }

        await Task.WhenAll(loads).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads one image, reusing an in-flight or finished request for the same source.
    /// </summary>
    /// <param name="source">Value of the <c>src</c> attribute.</param>
    /// <param name="cancellationToken">Token that aborts the download.</param>
    /// <returns>The decoded image, or <c>null</c> when it cannot be loaded.</returns>
    public Task<HtmlImage?> LoadAsync(string source, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source) || _disposed)
        {
            return Task.FromResult<HtmlImage?>(null);
        }

        lock (_gate)
        {
            if (_pending.TryGetValue(source, out Task<HtmlImage?>? pending))
            {
                return pending;
            }

            Task<HtmlImage?> task = LoadCoreAsync(source, cancellationToken);
            _pending[source] = task;
            return task;
        }
    }

    /// <summary>
    /// Releases every decoded image owned by the loader.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        lock (_gate)
        {
            if (_cache is null)
            {
                // Without a cache this loader owns everything it produced.
                foreach (HtmlImage image in _images.Values)
                {
                    image.Dispose();
                }
            }
            else
            {
                // The cache owns the images; this render only gives its references back.
                foreach (string source in _images.Keys)
                {
                    _cache.Release(source);
                }
            }

            _images.Clear();
            _pending.Clear();
        }
    }

    /// <summary>
    /// Performs the actual fetch, decode and book-keeping for one source.
    /// </summary>
    /// <param name="source">Raw <c>src</c> value.</param>
    /// <param name="cancellationToken">Token that aborts the download.</param>
    /// <returns>The decoded image, or <c>null</c> on any failure.</returns>
    private async Task<HtmlImage?> LoadCoreAsync(string source, CancellationToken cancellationToken)
    {
        HtmlImage? image = null;

        try
        {
            // A cache hit skips the download and the decode entirely.
            if (_cache is not null && _cache.TryAcquire(source, out HtmlImage? cached))
            {
                image = cached;
            }
            else
            {
                byte[]? bytes = await ReadBytesAsync(source, cancellationToken).ConfigureAwait(false);

                if (bytes is { Length: > 0 })
                {
                    HtmlImage? loaded = CreateImage(bytes);

                    if (loaded is not null)
                    {
                        // The cache owns the instance and returns the image this render must use,
                        // which is the already cached one when a parallel load won the race.
                        image = _cache?.AddOrAcquire(source, loaded) ?? loaded;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // A broken image must never abort the whole render.
            image = null;
        }

        if (image is not null)
        {
            lock (_gate)
            {
                _images[source] = image;
            }
        }

        return image;
    }

    /// <summary>
    /// Reads the raw bytes behind a source string.
    /// </summary>
    /// <param name="source">Raw <c>src</c> value.</param>
    /// <param name="cancellationToken">Token that aborts the read.</param>
    /// <returns>The bytes, or <c>null</c> when the source cannot be resolved.</returns>
    private async Task<byte[]?> ReadBytesAsync(string source, CancellationToken cancellationToken)
    {
        if (DataUriParser.TryDecode(source, out byte[] inlineData, out _))
        {
            return inlineData;
        }

        string resolved = ResolveSource(source);

        if (resolved.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            resolved.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!_options.LoadRemoteImages)
            {
                return null;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.ImageLoadTimeout);

            using HttpResponseMessage response = await Client
                .GetAsync(resolved, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false);
        }

        string? path = ResolveFilePath(resolved);
        return path is not null && File.Exists(path)
            ? await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)
            : null;
    }

    /// <summary>
    /// Turns a relative source into an absolute URL or path using <see cref="HtmlRendererOptions.BaseUri"/>.
    /// </summary>
    /// <param name="source">Raw <c>src</c> value.</param>
    /// <returns>The resolved value; the input is returned unchanged when it cannot be combined.</returns>
    private string ResolveSource(string source)
    {
        if (Path.IsPathRooted(source) ||
            source.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUri) ||
            !Uri.TryCreate(_options.BaseUri, UriKind.Absolute, out Uri? baseUri))
        {
            return source;
        }

        if (baseUri.Scheme is "http" or "https" && Uri.TryCreate(baseUri, source, out Uri? absolute))
        {
            return absolute.AbsoluteUri;
        }

        if (baseUri.IsFile)
        {
            string directory = Path.GetDirectoryName(baseUri.LocalPath) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(directory, source));
        }

        return source;
    }

    /// <summary>
    /// Converts a <c>file:</c> URI or a plain path into a local file path.
    /// </summary>
    /// <param name="source">Resolved source value.</param>
    /// <returns>The local path, or <c>null</c> when the value is not a file location.</returns>
    private static string? ResolveFilePath(string source)
    {
        if (source.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return Uri.TryCreate(source, UriKind.Absolute, out Uri? uri) ? uri.LocalPath : null;
        }

        return Path.IsPathRooted(source) ? source : null;
    }

    /// <summary>
    /// Creates an image from its encoded bytes, trying the raster codecs first and then SVG parsing.
    /// </summary>
    /// <param name="bytes">Encoded image data such as PNG, JPEG or SVG.</param>
    /// <returns>The image, or <c>null</c> when the format is not supported.</returns>
    private static HtmlImage? CreateImage(byte[] bytes)
    {
        // Skia's codecs cannot decode SVG, so a vector document is carried as a picture instead of
        // an SKImage; only the raster path goes through FromEncodedData.
        using (SKData data = SKData.CreateCopy(bytes))
        {
            HtmlImage? raster = HtmlImage.FromBitmap(SKImage.FromEncodedData(data));

            if (raster is not null)
            {
                return raster;
            }
        }

        return HtmlImage.FromSvg(bytes);
    }

    /// <summary>
    /// Creates the shared HTTP client used for remote images.
    /// </summary>
    /// <returns>A client with connection pooling and no global timeout, because each request
    /// carries its own deadline.</returns>
    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HtmlToImageRenderer/1.0");
        return client;
    }
}
