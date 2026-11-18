using SkiaSharp;
using Svg.Skia;

namespace HtmlToImageRenderer.Resources;

/// <summary>
/// An image referenced by the document. It is either a decoded raster image (PNG, JPEG, GIF, BMP,
/// WEBP, ICO) or an SVG, which is kept as a picture and rasterized by Skia at paint time so that it
/// stays crisp at any size. Skia's built-in codecs cannot decode SVG, which is why the two cases are
/// modelled separately instead of forcing everything into an <see cref="SKImage"/>.
/// </summary>
public sealed class HtmlImage : IDisposable
{
    private readonly SKImage? _raster;
    private readonly SKSvg? _svg;
    private readonly SKPicture? _picture;
    private readonly SKRect _bounds;
    private bool _disposed;

    /// <summary>
    /// Initializes a raster image.
    /// </summary>
    /// <param name="raster">Decoded raster image, owned by this instance.</param>
    private HtmlImage(SKImage raster)
    {
        _raster = raster;
        Width = raster.Width;
        Height = raster.Height;
    }

    /// <summary>
    /// Initializes an SVG image.
    /// </summary>
    /// <param name="svg">Loaded SVG document, owned by this instance and kept alive because the
    /// picture is built from its internal model.</param>
    /// <param name="picture">Picture describing the vector content.</param>
    /// <param name="bounds">Intrinsic bounds of the picture, used as the intrinsic size.</param>
    private HtmlImage(SKSvg svg, SKPicture picture, SKRect bounds)
    {
        _svg = svg;
        _picture = picture;
        _bounds = bounds;
        Width = bounds.Width;
        Height = bounds.Height;
    }

    /// <summary>
    /// Gets the intrinsic width in pixels. For an SVG this is the width of its picture bounds.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the intrinsic height in pixels. For an SVG this is the height of its picture bounds.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Wraps a decoded raster image.
    /// </summary>
    /// <param name="image">Decoded image, or <c>null</c> when decoding failed.</param>
    /// <returns>The wrapper, or <c>null</c> when the image has no usable size.</returns>
    public static HtmlImage? FromBitmap(SKImage? image) =>
        image is null || image.Width <= 0 || image.Height <= 0 ? null : new HtmlImage(image);

    /// <summary>
    /// Parses SVG markup and wraps it as a vector image.
    /// </summary>
    /// <param name="bytes">Encoded SVG content.</param>
    /// <returns>The wrapper, or <c>null</c> when the content is not a usable SVG.</returns>
    public static HtmlImage? FromSvg(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        SKSvg? svg = null;

        try
        {
            svg = new SKSvg();

            using (var stream = new MemoryStream(bytes, writable: false))
            {
                svg.Load(stream);
            }

            SKPicture? picture = svg.Picture;

            if (picture is null)
            {
                svg.Dispose();
                return null;
            }

            SKRect bounds = picture.CullRect;

            // A picture without bounds cannot contribute an intrinsic size, so the element falls
            // back to its alt text instead of rendering nothing at an unknown size.
            if (bounds.Width <= 0f || bounds.Height <= 0f)
            {
                svg.Dispose();
                return null;
            }

            return new HtmlImage(svg, picture, bounds);
        }
        catch
        {
            svg?.Dispose();
            return null;
        }
    }

    /// <summary>
    /// Draws the image into the given rectangle.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="destination">Destination rectangle in canvas coordinates.</param>
    /// <param name="sampling">Sampling options used for raster images.</param>
    /// <param name="paint">Paint carrying the alpha and anti-aliasing settings.</param>
    public void Draw(SKCanvas canvas, SKRect destination, SKSamplingOptions sampling, SKPaint paint)
    {
        if (_disposed || destination.Width <= 0f || destination.Height <= 0f)
        {
            return;
        }

        if (_raster is not null)
        {
            canvas.DrawImage(_raster, destination, sampling, paint);
            return;
        }

        if (_picture is null)
        {
            return;
        }

        canvas.Save();

        // Clip so an SVG whose content spills outside its view box cannot bleed over neighbours.
        canvas.ClipRect(destination, SKClipOperation.Intersect, true);
        canvas.Translate(destination.Left, destination.Top);

        // A non-uniform scale mirrors what DrawImage does for rasters: the layout has already
        // preserved the aspect ratio, and vector output stays sharp at any factor.
        canvas.Scale(destination.Width / _bounds.Width, destination.Height / _bounds.Height);
        canvas.Translate(-_bounds.Left, -_bounds.Top);
        canvas.DrawPicture(_picture);

        canvas.Restore();
    }

    /// <summary>
    /// Releases the owned raster image or SVG document.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _raster?.Dispose();
        _svg?.Dispose();
    }
}
