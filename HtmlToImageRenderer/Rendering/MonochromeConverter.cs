using System.Runtime.InteropServices;
using SkiaSharp;

namespace HtmlToImageRenderer.Rendering;

/// <summary>
/// Converts a rendered bitmap to a two-level (black and white) 8-bit grayscale bitmap, which is what
/// receipt printers expect. SkiaSharp has no 1-bit colour type, so the result is a <c>Gray_8</c>
/// bitmap whose pixels are only ever 0 or 255.
/// </summary>
public static class MonochromeConverter
{
    /// <summary>
    /// Converts the source bitmap to black and white using a fixed 50% threshold on BT.601 luminance.
    /// </summary>
    /// <param name="source">Fully composited bitmap; the caller keeps ownership of it.</param>
    /// <returns>A new <c>Gray_8</c> bitmap owned by the caller.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public static SKBitmap ToMonochrome(SKBitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int width = source.Width;
        int height = source.Height;

        var info = new SKImageInfo(width, height, SKColorType.Gray8, SKAlphaType.Opaque);
        var result = new SKBitmap(info);

        SKColor[] pixels = source.Pixels;

        if (pixels.Length < width * height)
        {
            // Defensive: an unreadable bitmap yields an empty white result instead of throwing.
            return result;
        }

        var gray = new byte[width * height];

        for (int index = 0; index < gray.Length; index++)
        {
            SKColor pixel = pixels[index];

            // The canvas was cleared with the background colour before painting, so every pixel is
            // already composited and the alpha channel needs no separate treatment.
            float luminance = (0.299f * pixel.Red) + (0.587f * pixel.Green) + (0.114f * pixel.Blue);

            gray[index] = luminance < 128f ? (byte)0 : (byte)255;
        }

        IntPtr destination = result.GetPixels();
        int rowBytes = result.RowBytes;

        // Copy row by row because the destination stride is not guaranteed to equal the width.
        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(gray, y * width, IntPtr.Add(destination, y * rowBytes), width);
        }

        return result;
    }
}
