namespace HtmlToImageRenderer.Css;

/// <summary>
/// Main axis of a flex container (value of the <c>flex-direction</c> property).
/// </summary>
public enum FlexDirection
{
    /// <summary>Main axis runs left to right.</summary>
    Row,

    /// <summary>Main axis runs right to left.</summary>
    RowReverse,

    /// <summary>Main axis runs top to bottom.</summary>
    Column,

    /// <summary>Main axis runs bottom to top.</summary>
    ColumnReverse,
}
