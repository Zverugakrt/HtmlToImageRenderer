namespace HtmlToImageRenderer.Css;

/// <summary>
/// Alignment of flex items along the container's cross axis
/// (value of the <c>align-items</c> / <c>align-self</c> properties).
/// </summary>
public enum AlignItems
{
    /// <summary>Align to the cross-start edge.</summary>
    FlexStart,

    /// <summary>Align to the cross-end edge.</summary>
    FlexEnd,

    /// <summary>Center on the cross axis.</summary>
    Center,

    /// <summary>Stretch the item to fill the cross size of the line.</summary>
    Stretch,

    /// <summary>Align baselines. Rendered like <see cref="FlexStart"/> for mixed content.</summary>
    Baseline,
}
