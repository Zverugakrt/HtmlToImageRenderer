namespace HtmlToImageRenderer.Css;

/// <summary>
/// A CSS length that keeps its unit until layout can resolve it, because percentages
/// depend on the containing block and <c>em</c>/<c>rem</c> depend on font sizes.
/// </summary>
public readonly struct CssLength : IEquatable<CssLength>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssLength"/> struct.
    /// </summary>
    /// <param name="value">Numeric part of the length.</param>
    /// <param name="unit">Unit the numeric part is expressed in.</param>
    public CssLength(float value, LengthUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Gets the numeric part of the length.
    /// </summary>
    public float Value { get; }

    /// <summary>
    /// Gets the unit the value is expressed in.
    /// </summary>
    public LengthUnit Unit { get; }

    /// <summary>
    /// Gets a value indicating whether the length is the <c>auto</c> keyword.
    /// </summary>
    public bool IsAuto => Unit == LengthUnit.Auto;

    /// <summary>
    /// Gets the <c>auto</c> keyword length.
    /// </summary>
    public static CssLength Auto => new(0f, LengthUnit.Auto);

    /// <summary>
    /// Gets a zero pixel length.
    /// </summary>
    public static CssLength Zero => new(0f, LengthUnit.Px);

    /// <summary>
    /// Creates a length expressed in CSS pixels.
    /// </summary>
    /// <param name="value">Pixel count.</param>
    /// <returns>The new length.</returns>
    public static CssLength FromPixels(float value) => new(value, LengthUnit.Px);

    /// <summary>
    /// Creates a percentage length.
    /// </summary>
    /// <param name="value">Percentage value, for example <c>50</c> for <c>50%</c>.</param>
    /// <returns>The new length.</returns>
    public static CssLength FromPercent(float value) => new(value, LengthUnit.Percent);

    /// <summary>
    /// Converts the length to absolute pixels for the given layout context.
    /// </summary>
    /// <param name="percentBase">Value that a percentage resolves against (usually the containing block width).</param>
    /// <param name="fontSize">Font size of the element, used by <c>em</c>.</param>
    /// <param name="rootFontSize">Root font size, used by <c>rem</c>.</param>
    /// <returns>The resolved pixel value; <c>auto</c> resolves to zero.</returns>
    public float Resolve(float percentBase, float fontSize, float rootFontSize) => Unit switch
    {
        LengthUnit.Auto => 0f,
        LengthUnit.Px => Value,
        LengthUnit.Percent => percentBase * Value / 100f,
        LengthUnit.Em => Value * fontSize,
        LengthUnit.Rem => Value * rootFontSize,
        LengthUnit.Pt => Value * 96f / 72f,
        LengthUnit.Unitless => Value,
        _ => 0f,
    };

    /// <inheritdoc />
    public bool Equals(CssLength other) => Value.Equals(other.Value) && Unit == other.Unit;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CssLength other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Value, Unit);

    /// <inheritdoc />
    public override string ToString() => Unit switch
    {
        LengthUnit.Auto => "auto",
        LengthUnit.Percent => $"{Value}%",
        LengthUnit.Em => $"{Value}em",
        LengthUnit.Rem => $"{Value}rem",
        LengthUnit.Pt => $"{Value}pt",
        LengthUnit.Unitless => Value.ToString(),
        _ => $"{Value}px",
    };

    /// <summary>
    /// Compares two lengths for equality.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><c>true</c> when value and unit match.</returns>
    public static bool operator ==(CssLength left, CssLength right) => left.Equals(right);

    /// <summary>
    /// Compares two lengths for inequality.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><c>true</c> when value or unit differ.</returns>
    public static bool operator !=(CssLength left, CssLength right) => !left.Equals(right);
}
