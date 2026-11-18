using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Layout;
using SkiaSharp;

namespace HtmlToImageRenderer.Rendering;

/// <summary>
/// Paints a laid out box tree onto a Skia canvas. Boxes are drawn in CSS paint order:
/// background, borders, replaced image, text lines, then children. Border radii round the
/// background and the border ring, and clip the content of the box.
/// </summary>
public sealed class HtmlPainter : IDisposable
{
    private static readonly SKSamplingOptions ImageSampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private readonly SKPaint _fillPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private readonly float _rootFontSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlPainter"/> class.
    /// </summary>
    /// <param name="rootFontSize">
    /// Root font size in pixels, used to resolve <c>rem</c> based corner radii.
    /// </param>
    public HtmlPainter(float rootFontSize = 16f)
    {
        _rootFontSize = rootFontSize > 0f ? rootFontSize : 16f;
    }

    /// <summary>
    /// Paints a whole document tree.
    /// </summary>
    /// <param name="canvas">Target canvas; its transform is used as-is.</param>
    /// <param name="root">Root box produced by the layout engine.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public void Paint(SKCanvas canvas, LayoutBox root)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(root);

        PaintBox(canvas, root, 0f, 0f);
    }

    /// <summary>
    /// Releases the cached paints.
    /// </summary>
    public void Dispose()
    {
        _fillPaint.Dispose();
        _borderPaint.Dispose();
    }

    /// <summary>
    /// Paints one box and, recursively, its children.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="box">Box to paint.</param>
    /// <param name="parentContentX">Absolute x of the parent's content origin.</param>
    /// <param name="parentContentY">Absolute y of the parent's content origin.</param>
    private void PaintBox(SKCanvas canvas, LayoutBox box, float parentContentX, float parentContentY)
    {
        float x = parentContentX + box.X;
        float y = parentContentY + box.Y;
        float contentX = x + box.Border.Left + box.Padding.Left;
        float contentY = y + box.Border.Top + box.Padding.Top;

        // A null round rect means the box is painted as a plain rectangle, which is also the
        // signal that content must not be clipped.
        SKRoundRect? outer = CreateRoundRect(box, x, y, includeBorder: true);

        if (box.Width > 0f && box.Height > 0f && box.Style.BackgroundColor.A > 0)
        {
            _fillPaint.Color = box.Style.BackgroundColor.ToSKColor();

            if (outer is not null)
            {
                canvas.DrawRoundRect(outer, _fillPaint);
            }
            else
            {
                canvas.DrawRect(x, y, box.Width, box.Height, _fillPaint);
            }
        }

        DrawBorders(canvas, box, x, y, outer);

        if (outer is not null)
        {
            canvas.Save();

            SKRoundRect? innerClip = CreateRoundRect(box, x, y, includeBorder: false);

            if (innerClip is not null)
            {
                canvas.ClipRoundRect(innerClip, SKClipOperation.Intersect, true);
                innerClip.Dispose();
            }
        }

        if (box.Image is not null && box.ContentWidth > 0f && box.ContentHeight > 0f)
        {
            // White keeps the alpha channel of a raster intact and carries the anti-aliasing
            // settings into the vector path.
            _fillPaint.Color = SKColors.White;
            box.Image.Draw(
                canvas,
                new SKRect(contentX, contentY, contentX + box.ContentWidth, contentY + box.ContentHeight),
                ImageSampling,
                _fillPaint);
        }

        DrawLines(canvas, box, contentX, contentY);

        foreach (LayoutBox child in box.Children)
        {
            PaintBox(canvas, child, contentX, contentY);
        }

        if (outer is not null)
        {
            canvas.Restore();
        }

        outer?.Dispose();
    }

    /// <summary>
    /// Draws the text lines of a box.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="box">Box whose lines are painted.</param>
    /// <param name="contentX">Absolute x of the box's content origin.</param>
    /// <param name="contentY">Absolute y of the box's content origin.</param>
    private void DrawLines(SKCanvas canvas, LayoutBox box, float contentX, float contentY)
    {
        foreach (TextLine line in box.Lines)
        {
            float baselineY = contentY + line.Top + line.Baseline;

            foreach (TextFragment fragment in line.Fragments)
            {
                float fragmentX = contentX + line.X + fragment.X;

                if (fragment.Image is not null)
                {
                    // Inline images sit on the baseline, so the box grows upwards from it.
                    float top = baselineY - fragment.Height;
                    _fillPaint.Color = SKColors.White;
                    fragment.Image.Draw(
                        canvas,
                        new SKRect(fragmentX, top, fragmentX + fragment.Width, top + fragment.Height),
                        ImageSampling,
                        _fillPaint);
                    continue;
                }

                if (fragment.Font is null || string.IsNullOrEmpty(fragment.Text))
                {
                    continue;
                }

                _fillPaint.Color = fragment.Color.ToSKColor();
                canvas.DrawText(fragment.Text, fragmentX, baselineY, SKTextAlign.Left, fragment.Font, _fillPaint);

                if (fragment.Underline)
                {
                    float thickness = MathF.Max(1f, fragment.Font.Size / 14f);
                    canvas.DrawRect(fragmentX, baselineY + 1f, fragment.Width, thickness, _fillPaint);
                }
            }
        }
    }

    /// <summary>
    /// Draws the four border sides, each with its own style and color.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="box">Box whose borders are drawn.</param>
    /// <param name="x">Absolute x of the border box.</param>
    /// <param name="y">Absolute y of the border box.</param>
    /// <param name="outer">Border-box round rect, or <c>null</c> when all corners are square.</param>
    private void DrawBorders(SKCanvas canvas, LayoutBox box, float x, float y, SKRoundRect? outer)
    {
        ComputedStyle style = box.Style;

        if (!style.HasVisibleBorder)
        {
            return;
        }

        if (outer is not null)
        {
            DrawRoundedBorders(canvas, box, x, y, outer);
            return;
        }

        float right = x + box.Width;
        float bottom = y + box.Height;

        if (box.Border.Top > 0f)
        {
            DrawBorderSide(canvas, box.Border.Top, style.BorderTopStyle, style.BorderTopColor.ToSKColor(), x, y, right, y);
        }

        if (box.Border.Bottom > 0f)
        {
            DrawBorderSide(canvas, box.Border.Bottom, style.BorderBottomStyle, style.BorderBottomColor.ToSKColor(), x, bottom, right, bottom);
        }

        if (box.Border.Left > 0f)
        {
            DrawBorderSide(canvas, box.Border.Left, style.BorderLeftStyle, style.BorderLeftColor.ToSKColor(), x, y, x, bottom);
        }

        if (box.Border.Right > 0f)
        {
            DrawBorderSide(canvas, box.Border.Right, style.BorderRightStyle, style.BorderRightColor.ToSKColor(), right, y, right, bottom);
        }
    }

    /// <summary>
    /// Draws borders of a rounded box: the ring between the border box and the padding box is
    /// filled once per side, clipped to that side's band.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="box">Box whose borders are drawn.</param>
    /// <param name="x">Absolute x of the border box.</param>
    /// <param name="y">Absolute y of the border box.</param>
    /// <param name="outer">Border-box round rect.</param>
    private void DrawRoundedBorders(SKCanvas canvas, LayoutBox box, float x, float y, SKRoundRect outer)
    {
        SKRoundRect? inner = CreateRoundRect(box, x, y, includeBorder: false);

        if (inner is null)
        {
            return;
        }

        // Even-odd fill of two nested contours yields the border ring.
        using var ring = new SKPath { FillType = SKPathFillType.EvenOdd };
        ring.AddRoundRect(outer);
        ring.AddRoundRect(inner);
        inner.Dispose();

        ComputedStyle style = box.Style;
        float width = box.Width;
        float height = box.Height;

        DrawBorderBand(canvas, ring, box.Border.Top, style.BorderTopStyle, style.BorderTopColor, SKRect.Create(x, y, width, box.Border.Top));
        DrawBorderBand(canvas, ring, box.Border.Bottom, style.BorderBottomStyle, style.BorderBottomColor, SKRect.Create(x, y + height - box.Border.Bottom, width, box.Border.Bottom));
        DrawBorderBand(canvas, ring, box.Border.Left, style.BorderLeftStyle, style.BorderLeftColor, SKRect.Create(x, y, box.Border.Left, height));
        DrawBorderBand(canvas, ring, box.Border.Right, style.BorderRightStyle, style.BorderRightColor, SKRect.Create(x + width - box.Border.Right, y, box.Border.Right, height));
    }

    /// <summary>
    /// Fills the border ring inside one side's band with that side's color.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="ring">Border ring path shared by all sides.</param>
    /// <param name="thickness">Width of the side.</param>
    /// <param name="borderStyle">Line style of the side.</param>
    /// <param name="color">Color of the side.</param>
    /// <param name="band">Area the side may paint into.</param>
    private void DrawBorderBand(
        SKCanvas canvas,
        SKPath ring,
        float thickness,
        BorderStyleKind borderStyle,
        CssColor color,
        SKRect band)
    {
        if (thickness <= 0f || borderStyle == BorderStyleKind.None || band.Width <= 0f || band.Height <= 0f)
        {
            return;
        }

        // The ring is a filled area, so dashed and dotted sides are painted solid here. A corner
        // arc belongs to two bands and therefore takes the color of the side painted last.
        canvas.Save();
        canvas.ClipRect(band);
        _fillPaint.Color = color.ToSKColor();
        canvas.DrawPath(ring, _fillPaint);
        canvas.Restore();
    }

    /// <summary>
    /// Draws a single border side of a square-cornered box, either as a filled rectangle or as a
    /// dashed/dotted line.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="thickness">Border width in pixels.</param>
    /// <param name="borderStyle">Line style of the border.</param>
    /// <param name="color">Border color.</param>
    /// <param name="x1">Start x of the side.</param>
    /// <param name="y1">Start y of the side.</param>
    /// <param name="x2">End x of the side.</param>
    /// <param name="y2">End y of the side.</param>
    private void DrawBorderSide(
        SKCanvas canvas,
        float thickness,
        BorderStyleKind borderStyle,
        SKColor color,
        float x1,
        float y1,
        float x2,
        float y2)
    {
        if (borderStyle == BorderStyleKind.None)
        {
            return;
        }

        if (borderStyle == BorderStyleKind.Solid)
        {
            float width = MathF.Abs(x2 - x1);
            float height = MathF.Abs(y2 - y1);

            if (width < 0.01f)
            {
                width = thickness;
            }

            if (height < 0.01f)
            {
                height = thickness;
            }

            _fillPaint.Color = color;
            canvas.DrawRect(
                SKRect.Create(MathF.Min(x1, x2), MathF.Min(y1, y2), width, height),
                _fillPaint);
            return;
        }

        float[] intervals = borderStyle == BorderStyleKind.Dotted
            ? [thickness, thickness * 2f]
            : [thickness * 3f, thickness * 2f];

        using SKPathEffect effect = SKPathEffect.CreateDash(intervals, 0f);

        _borderPaint.Color = color;
        _borderPaint.StrokeWidth = thickness;
        _borderPaint.PathEffect = effect;
        canvas.DrawLine(x1, y1, x2, y2, _borderPaint);
        _borderPaint.PathEffect = null;
    }

    /// <summary>
    /// Builds the round rect of a box, resolving radii to pixels and applying the CSS rule that
    /// prevents opposing corners from overlapping.
    /// </summary>
    /// <param name="box">Box whose radii are resolved.</param>
    /// <param name="x">Absolute x of the border box.</param>
    /// <param name="y">Absolute y of the border box.</param>
    /// <param name="includeBorder">
    /// When <c>true</c> the border box is described; when <c>false</c> the padding box is described
    /// and the radii are reduced by the border widths.
    /// </param>
    /// <returns>The round rect, or <c>null</c> when all corners are square or the box is empty.</returns>
    private SKRoundRect? CreateRoundRect(LayoutBox box, float x, float y, bool includeBorder)
    {
        CssBorderRadius radius = box.Style.BorderRadius;

        if (radius.IsZero || box.Width <= 0f || box.Height <= 0f)
        {
            return null;
        }

        float fontSize = box.Style.FontSize;
        float width = box.Width;
        float height = box.Height;

        // Percentages resolve against the box size, so horizontal and vertical radii use different
        // bases, exactly like CSS does.
        float topLeftX = radius.TopLeftX.Resolve(width, fontSize, _rootFontSize);
        float topLeftY = radius.TopLeftY.Resolve(height, fontSize, _rootFontSize);
        float topRightX = radius.TopRightX.Resolve(width, fontSize, _rootFontSize);
        float topRightY = radius.TopRightY.Resolve(height, fontSize, _rootFontSize);
        float bottomRightX = radius.BottomRightX.Resolve(width, fontSize, _rootFontSize);
        float bottomRightY = radius.BottomRightY.Resolve(height, fontSize, _rootFontSize);
        float bottomLeftX = radius.BottomLeftX.Resolve(width, fontSize, _rootFontSize);
        float bottomLeftY = radius.BottomLeftY.Resolve(height, fontSize, _rootFontSize);

        float factor = 1f;
        factor = ScaleFactor(factor, width, topLeftX + topRightX);
        factor = ScaleFactor(factor, width, bottomLeftX + bottomRightX);
        factor = ScaleFactor(factor, height, topLeftY + bottomLeftY);
        factor = ScaleFactor(factor, height, topRightY + bottomRightY);

        if (factor < 1f)
        {
            topLeftX *= factor;
            topLeftY *= factor;
            topRightX *= factor;
            topRightY *= factor;
            bottomRightX *= factor;
            bottomRightY *= factor;
            bottomLeftX *= factor;
            bottomLeftY *= factor;
        }

        float left = x;
        float top = y;
        float right = x + width;
        float bottom = y + height;

        if (!includeBorder)
        {
            // The inner contour is shrunk by the border widths, and its radii shrink with it.
            left += box.Border.Left;
            top += box.Border.Top;
            right -= box.Border.Right;
            bottom -= box.Border.Bottom;

            topLeftX = MathF.Max(0f, topLeftX - box.Border.Left);
            topLeftY = MathF.Max(0f, topLeftY - box.Border.Top);
            topRightX = MathF.Max(0f, topRightX - box.Border.Right);
            topRightY = MathF.Max(0f, topRightY - box.Border.Top);
            bottomRightX = MathF.Max(0f, bottomRightX - box.Border.Right);
            bottomRightY = MathF.Max(0f, bottomRightY - box.Border.Bottom);
            bottomLeftX = MathF.Max(0f, bottomLeftX - box.Border.Left);
            bottomLeftY = MathF.Max(0f, bottomLeftY - box.Border.Bottom);
        }

        if (right <= left || bottom <= top)
        {
            return null;
        }

        var roundRect = new SKRoundRect();
        roundRect.SetRectRadii(
            new SKRect(left, top, right, bottom),
            [
                new SKPoint(topLeftX, topLeftY),
                new SKPoint(topRightX, topRightY),
                new SKPoint(bottomRightX, bottomRightY),
                new SKPoint(bottomLeftX, bottomLeftY),
            ]);

        return roundRect;
    }

    /// <summary>
    /// Lowers the overlap factor so that two opposing radii never exceed the available size.
    /// </summary>
    /// <param name="current">Factor computed so far.</param>
    /// <param name="available">Box width or height.</param>
    /// <param name="used">Sum of the two radii that share that axis.</param>
    /// <returns>The updated factor.</returns>
    private static float ScaleFactor(float current, float available, float used) =>
        used > 0.0001f ? MathF.Min(current, available / used) : current;
}
