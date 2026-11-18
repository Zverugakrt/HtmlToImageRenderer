using System.Globalization;
using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;
using HtmlToImageRenderer.Resources;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Computes the used size of a replaced element. CSS wins over HTML attributes, attributes
/// win over the intrinsic image size, and a single known dimension preserves the aspect ratio
/// of the decoded image.
/// </summary>
internal static class ImageSizing
{
    /// <summary>
    /// Computes the used size of an <c>&lt;img&gt;</c> element.
    /// </summary>
    /// <param name="context">Layout context providing image lookup and length resolution.</param>
    /// <param name="element">Element carrying the <c>src</c> attribute.</param>
    /// <param name="style">Computed style of the element.</param>
    /// <param name="percentBase">Value that percentage sizes resolve against.</param>
    /// <param name="width">Used width in pixels on success.</param>
    /// <param name="height">Used height in pixels on success.</param>
    /// <returns><c>false</c> when the element has no usable source or no usable size.</returns>
    public static bool TryGetSize(
        LayoutContext context,
        HtmlElement element,
        ComputedStyle style,
        float percentBase,
        out float width,
        out float height)
    {
        width = 0f;
        height = 0f;

        string? source = element.GetAttribute("src");
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        HtmlImage? image = context.GetImage(source);
        float intrinsicWidth = image?.Width ?? 0f;
        float intrinsicHeight = image?.Height ?? 0f;

        float? cssWidth = style.Width.IsAuto ? null : context.ResolveLength(style.Width, percentBase, style);
        float? cssHeight = style.Height.IsAuto ? null : context.ResolveLength(style.Height, percentBase, style);
        float? attributeWidth = ParseDimension(element.GetAttribute("width"));
        float? attributeHeight = ParseDimension(element.GetAttribute("height"));

        float resolvedWidth = cssWidth ?? attributeWidth ?? intrinsicWidth;
        float resolvedHeight = cssHeight ?? attributeHeight ?? intrinsicHeight;

        if (cssWidth.HasValue && !cssHeight.HasValue)
        {
            resolvedHeight = ScaleFromWidth(resolvedWidth, intrinsicWidth, intrinsicHeight);
        }
        else if (cssHeight.HasValue && !cssWidth.HasValue)
        {
            resolvedWidth = ScaleFromHeight(resolvedHeight, intrinsicWidth, intrinsicHeight);
        }
        else if (!cssWidth.HasValue && !cssHeight.HasValue)
        {
            // Only HTML attributes are present: the missing side follows the intrinsic ratio.
            if (attributeWidth.HasValue && !attributeHeight.HasValue)
            {
                resolvedHeight = ScaleFromWidth(resolvedWidth, intrinsicWidth, intrinsicHeight);
            }
            else if (attributeHeight.HasValue && !attributeWidth.HasValue)
            {
                resolvedWidth = ScaleFromHeight(resolvedHeight, intrinsicWidth, intrinsicHeight);
            }
        }

        if (resolvedWidth <= 0f || resolvedHeight <= 0f)
        {
            return false;
        }

        width = resolvedWidth;
        height = resolvedHeight;
        return true;
    }

    /// <summary>
    /// Derives the height from a width while preserving the intrinsic aspect ratio.
    /// </summary>
    /// <param name="width">Known width.</param>
    /// <param name="intrinsicWidth">Image width, possibly zero when the image is unavailable.</param>
    /// <param name="intrinsicHeight">Image height, possibly zero.</param>
    /// <returns>The derived height, or zero when the ratio is unknown.</returns>
    private static float ScaleFromWidth(float width, float intrinsicWidth, float intrinsicHeight) =>
        intrinsicWidth > 0f ? width * intrinsicHeight / intrinsicWidth : 0f;

    /// <summary>
    /// Derives the width from a height while preserving the intrinsic aspect ratio.
    /// </summary>
    /// <param name="height">Known height.</param>
    /// <param name="intrinsicWidth">Image width, possibly zero when the image is unavailable.</param>
    /// <param name="intrinsicHeight">Image height, possibly zero.</param>
    /// <returns>The derived width, or zero when the ratio is unknown.</returns>
    private static float ScaleFromHeight(float height, float intrinsicWidth, float intrinsicHeight) =>
        intrinsicHeight > 0f ? height * intrinsicWidth / intrinsicHeight : 0f;

    /// <summary>
    /// Parses a presentational <c>width</c> or <c>height</c> attribute.
    /// </summary>
    /// <param name="value">Attribute value, optionally suffixed with <c>px</c>.</param>
    /// <returns>The pixel value, or <c>null</c> when the attribute is absent or invalid.</returns>
    private static float? ParseDimension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string text = value.Trim();
        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^2];
        }

        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) && result > 0f
            ? result
            : null;
    }
}
