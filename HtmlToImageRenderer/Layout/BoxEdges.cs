namespace HtmlToImageRenderer.Layout;

/// <summary>
/// The four sides of a margin, padding or border box, already resolved to pixels.
/// </summary>
public struct BoxEdges : IEquatable<BoxEdges>
{
    /// <summary>Gets or sets the top edge size in pixels.</summary>
    public float Top;

    /// <summary>Gets or sets the right edge size in pixels.</summary>
    public float Right;

    /// <summary>Gets or sets the bottom edge size in pixels.</summary>
    public float Bottom;

    /// <summary>Gets or sets the left edge size in pixels.</summary>
    public float Left;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoxEdges"/> struct.
    /// </summary>
    /// <param name="top">Top edge in pixels.</param>
    /// <param name="right">Right edge in pixels.</param>
    /// <param name="bottom">Bottom edge in pixels.</param>
    /// <param name="left">Left edge in pixels.</param>
    public BoxEdges(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>
    /// Gets edges of zero width.
    /// </summary>
    public static BoxEdges Zero => default;

    /// <summary>
    /// Gets the combined left and right size.
    /// </summary>
    public float Horizontal => Left + Right;

    /// <summary>
    /// Gets the combined top and bottom size.
    /// </summary>
    public float Vertical => Top + Bottom;

    /// <inheritdoc />
    public bool Equals(BoxEdges other) =>
        Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom) && Left.Equals(other.Left);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BoxEdges other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Top, Right, Bottom, Left);

    /// <inheritdoc />
    public override string ToString() => $"{Top},{Right},{Bottom},{Left}";
}
