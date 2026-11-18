using SkiaSharp;

namespace HtmlToImageRenderer.Rendering;

/// <summary>
/// Crops a rendered bitmap down to the bounding box of its non-background pixels. This removes the
/// untouched area an auto-height document leaves behind when the real content is smaller than the
/// allocated canvas.
/// </summary>
public static class BackgroundTrimmer
{
    /// <summary>
    /// Crops the bitmap to the area that contains anything other than the background color.
    /// </summary>
    /// <param name="source">Rendered bitmap; it is never modified.</param>
    /// <param name="background">Color the canvas was cleared with.</param>
    /// <returns>
    /// A new cropped bitmap, or the <paramref name="source"/> instance when nothing can be cropped
    /// (content already touches every edge, or the bitmap is a single pixel). The caller disposes
    /// whichever bitmap it did not receive ownership of.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public static SKBitmap Trim(SKBitmap source, SKColor background)
    {
        ArgumentNullException.ThrowIfNull(source);

        int width = source.Width;
        int height = source.Height;

        if (width <= 1 || height <= 1)
        {
            return source;
        }

        SKColor[] pixels = source.Pixels;

        if (pixels.Length < width * height)
        {
            return source;
        }

        int top = FindFirstRow(pixels, width, height, background, fromEnd: false);

        if (top < 0)
        {
            // The whole bitmap is background: a single pixel is a valid, non-empty result.
            return CreateSolid(background);
        }

        int bottom = FindFirstRow(pixels, width, height, background, fromEnd: true);

        // Columns are searched only inside the row band, which keeps the scan O(width * height)
        // even for tall pages.
        int left = FindFirstColumn(pixels, width, background, top, bottom, fromEnd: false);
        int right = FindFirstColumn(pixels, width, background, top, bottom, fromEnd: true);

        if (top == 0 && left == 0 && bottom == height - 1 && right == width - 1)
        {
            return source;
        }

        int trimmedWidth = Math.Max(1, right - left + 1);
        int trimmedHeight = Math.Max(1, bottom - top + 1);

        var info = new SKImageInfo(trimmedWidth, trimmedHeight, source.ColorType, source.AlphaType, source.ColorSpace);
        var result = new SKBitmap(info);

        var trimmedPixels = new SKColor[trimmedWidth * trimmedHeight];

        for (int y = 0; y < trimmedHeight; y++)
        {
            Array.Copy(pixels, ((top + y) * width) + left, trimmedPixels, y * trimmedWidth, trimmedWidth);
        }

        result.Pixels = trimmedPixels;
        return result;
    }

    /// <summary>
    /// Finds the first row that contains a non-background pixel, scanning from either edge.
    /// </summary>
    /// <param name="pixels">Flattened pixel buffer of the source bitmap.</param>
    /// <param name="width">Bitmap width.</param>
    /// <param name="height">Bitmap height.</param>
    /// <param name="background">Color treated as transparent for cropping.</param>
    /// <param name="fromEnd">When <c>true</c> the scan starts at the last row.</param>
    /// <returns>The row index, or <c>-1</c> when every row is background.</returns>
    private static int FindFirstRow(SKColor[] pixels, int width, int height, SKColor background, bool fromEnd)
    {
        for (int step = 0; step < height; step++)
        {
            int y = fromEnd ? height - 1 - step : step;
            int rowStart = y * width;

            for (int x = 0; x < width; x++)
            {
                if (Differs(pixels[rowStart + x], background))
                {
                    return y;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the first column that contains a non-background pixel inside the given row band.
    /// </summary>
    /// <param name="pixels">Flattened pixel buffer of the source bitmap.</param>
    /// <param name="width">Bitmap width.</param>
    /// <param name="background">Color treated as transparent for cropping.</param>
    /// <param name="top">First row of the band.</param>
    /// <param name="bottom">Last row of the band.</param>
    /// <param name="fromEnd">When <c>true</c> the scan starts at the last column.</param>
    /// <returns>The column index, or <c>-1</c> when every column in the band is background.</returns>
    private static int FindFirstColumn(SKColor[] pixels, int width, SKColor background, int top, int bottom, bool fromEnd)
    {
        for (int step = 0; step < width; step++)
        {
            int x = fromEnd ? width - 1 - step : step;

            for (int y = top; y <= bottom; y++)
            {
                if (Differs(pixels[(y * width) + x], background))
                {
                    return x;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Creates a one pixel bitmap filled with the given color.
    /// </summary>
    /// <param name="color">Color of the single pixel.</param>
    /// <returns>The new bitmap.</returns>
    private static SKBitmap CreateSolid(SKColor color)
    {
        var info = new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        bitmap.SetPixel(0, 0, color);
        return bitmap;
    }

    /// <summary>
    /// Compares a pixel with the background color. The comparison is exact on purpose: a tolerance
    /// would trim away content that is only slightly lighter than the background.
    /// </summary>
    /// <param name="pixel">Pixel from the rendered bitmap.</param>
    /// <param name="background">Color the canvas was cleared with.</param>
    /// <returns><c>true</c> when the pixel is not the background color.</returns>
    private static bool Differs(SKColor pixel, SKColor background) =>
        pixel.Red != background.Red ||
        pixel.Green != background.Green ||
        pixel.Blue != background.Blue ||
        pixel.Alpha != background.Alpha;
}
