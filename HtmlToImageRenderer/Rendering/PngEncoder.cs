using SkiaSharp;

namespace HtmlToImageRenderer.Rendering;

/// <summary>
/// Encodes a rasterized bitmap into the PNG byte array returned by the public API.
/// </summary>
public static class PngEncoder
{
    /// <summary>
    /// Encodes a bitmap as PNG.
    /// </summary>
    /// <param name="bitmap">Bitmap to encode; the caller keeps ownership of it.</param>
    /// <param name="quality">Encoder quality, 0-100. PNG is lossless, so this only affects effort.</param>
    /// <returns>The encoded PNG bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bitmap"/> is <c>null</c>.</exception>
    /// <exception cref="HtmlRenderException">Thrown when the encoder produces no data.</exception>
    public static byte[] EncodePng(SKBitmap bitmap, int quality = 100)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, Math.Clamp(quality, 0, 100));

        if (data is null || data.Size == 0)
        {
            throw new HtmlRenderException("The PNG encoder produced no data.");
        }

        return data.ToArray();
    }
}
