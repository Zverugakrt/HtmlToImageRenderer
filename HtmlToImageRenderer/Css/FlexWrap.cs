namespace HtmlToImageRenderer.Css;

/// <summary>
/// Wrapping behavior of a flex container (value of the <c>flex-wrap</c> property).
/// Only single-line layout is implemented, so <see cref="Wrap"/> and
/// <see cref="WrapReverse"/> currently behave like <see cref="NoWrap"/>.
/// </summary>
public enum FlexWrap
{
    /// <summary>Items stay on a single line and may shrink.</summary>
    NoWrap,

    /// <summary>Requested multi-line layout; rendered as a single line.</summary>
    Wrap,

    /// <summary>Requested reversed multi-line layout; rendered as a single line.</summary>
    WrapReverse,
}
