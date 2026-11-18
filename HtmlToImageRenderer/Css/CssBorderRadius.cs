namespace HtmlToImageRenderer.Css;

/// <summary>
/// Corner radii of a box. Every corner carries an independent horizontal and vertical radius,
/// which is what the <c>/</c> form of <c>border-radius</c> produces. Radii are kept as
/// <see cref="CssLength"/> because percentages resolve against the box size, which is only
/// known while painting.
/// </summary>
public readonly struct CssBorderRadius : IEquatable<CssBorderRadius>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssBorderRadius"/> struct.
    /// </summary>
    /// <param name="topLeftX">Horizontal radius of the top-left corner.</param>
    /// <param name="topLeftY">Vertical radius of the top-left corner.</param>
    /// <param name="topRightX">Horizontal radius of the top-right corner.</param>
    /// <param name="topRightY">Vertical radius of the top-right corner.</param>
    /// <param name="bottomRightX">Horizontal radius of the bottom-right corner.</param>
    /// <param name="bottomRightY">Vertical radius of the bottom-right corner.</param>
    /// <param name="bottomLeftX">Horizontal radius of the bottom-left corner.</param>
    /// <param name="bottomLeftY">Vertical radius of the bottom-left corner.</param>
    public CssBorderRadius(
        CssLength topLeftX,
        CssLength topLeftY,
        CssLength topRightX,
        CssLength topRightY,
        CssLength bottomRightX,
        CssLength bottomRightY,
        CssLength bottomLeftX,
        CssLength bottomLeftY)
    {
        TopLeftX = topLeftX;
        TopLeftY = topLeftY;
        TopRightX = topRightX;
        TopRightY = topRightY;
        BottomRightX = bottomRightX;
        BottomRightY = bottomRightY;
        BottomLeftX = bottomLeftX;
        BottomLeftY = bottomLeftY;
    }

    /// <summary>Gets the horizontal radius of the top-left corner.</summary>
    public CssLength TopLeftX { get; }

    /// <summary>Gets the vertical radius of the top-left corner.</summary>
    public CssLength TopLeftY { get; }

    /// <summary>Gets the horizontal radius of the top-right corner.</summary>
    public CssLength TopRightX { get; }

    /// <summary>Gets the vertical radius of the top-right corner.</summary>
    public CssLength TopRightY { get; }

    /// <summary>Gets the horizontal radius of the bottom-right corner.</summary>
    public CssLength BottomRightX { get; }

    /// <summary>Gets the vertical radius of the bottom-right corner.</summary>
    public CssLength BottomRightY { get; }

    /// <summary>Gets the horizontal radius of the bottom-left corner.</summary>
    public CssLength BottomLeftX { get; }

    /// <summary>Gets the vertical radius of the bottom-left corner.</summary>
    public CssLength BottomLeftY { get; }

    /// <summary>
    /// Gets a radius set where every corner is square.
    /// </summary>
    public static CssBorderRadius None => default;

    /// <summary>
    /// Gets a value indicating whether every corner radius is zero, in which case the box is
    /// painted as a plain rectangle.
    /// </summary>
    public bool IsZero =>
        IsZeroLength(TopLeftX) && IsZeroLength(TopLeftY) &&
        IsZeroLength(TopRightX) && IsZeroLength(TopRightY) &&
        IsZeroLength(BottomRightX) && IsZeroLength(BottomRightY) &&
        IsZeroLength(BottomLeftX) && IsZeroLength(BottomLeftY);

    /// <summary>
    /// Parses the <c>border-radius</c> shorthand, including the elliptical <c>/</c> form.
    /// </summary>
    /// <param name="text">
    /// Value such as <c>8px</c>, <c>4px 8px</c>, <c>10px / 20px</c> or <c>50%</c>.
    /// </param>
    /// <param name="radius">Parsed radii when the method succeeds.</param>
    /// <returns><c>true</c> when at least one valid radius list was parsed.</returns>
    public static bool TryParse(string? text, out CssBorderRadius radius)
    {
        radius = None;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] halves = text.Split('/', StringSplitOptions.TrimEntries);

        if (halves.Length is < 1 or > 2)
        {
            return false;
        }

        if (!TryParseCornerList(halves[0], out CssLength[] horizontal))
        {
            return false;
        }

        // Without an explicit second list the vertical radii equal the horizontal ones.
        CssLength[] vertical = horizontal;

        if (halves.Length == 2 && !TryParseCornerList(halves[1], out vertical))
        {
            return false;
        }

        radius = new CssBorderRadius(
            horizontal[0],
            vertical[0],
            horizontal[1],
            vertical[1],
            horizontal[2],
            vertical[2],
            horizontal[3],
            vertical[3]);

        return true;
    }

    /// <summary>
    /// Parses a single corner property such as <c>border-top-left-radius</c>.
    /// </summary>
    /// <param name="text">Value such as <c>8px</c> or <c>8px 4px</c>.</param>
    /// <param name="horizontal">Horizontal radius when the method succeeds.</param>
    /// <param name="vertical">Vertical radius when the method succeeds.</param>
    /// <returns><c>true</c> for one or two valid lengths.</returns>
    public static bool TryParseCorner(string? text, out CssLength horizontal, out CssLength vertical)
    {
        horizontal = CssLength.Zero;
        vertical = CssLength.Zero;

        string[] tokens = CssValueParser.SplitValues(text);

        if (tokens.Length is < 1 or > 2)
        {
            return false;
        }

        CssLength first = CssValueParser.ParseLength(tokens[0], CssLength.Auto);

        if (first.IsAuto)
        {
            return false;
        }

        if (tokens.Length == 1)
        {
            horizontal = first;
            vertical = first;
            return true;
        }

        CssLength second = CssValueParser.ParseLength(tokens[1], CssLength.Auto);

        if (second.IsAuto)
        {
            return false;
        }

        horizontal = first;
        vertical = second;
        return true;
    }

    /// <summary>
    /// Returns a copy with a different top-left corner.
    /// </summary>
    /// <param name="horizontal">Horizontal radius.</param>
    /// <param name="vertical">Vertical radius.</param>
    /// <returns>The updated radii.</returns>
    public CssBorderRadius WithTopLeft(CssLength horizontal, CssLength vertical) =>
        new(horizontal, vertical, TopRightX, TopRightY, BottomRightX, BottomRightY, BottomLeftX, BottomLeftY);

    /// <summary>
    /// Returns a copy with a different top-right corner.
    /// </summary>
    /// <param name="horizontal">Horizontal radius.</param>
    /// <param name="vertical">Vertical radius.</param>
    /// <returns>The updated radii.</returns>
    public CssBorderRadius WithTopRight(CssLength horizontal, CssLength vertical) =>
        new(TopLeftX, TopLeftY, horizontal, vertical, BottomRightX, BottomRightY, BottomLeftX, BottomLeftY);

    /// <summary>
    /// Returns a copy with a different bottom-right corner.
    /// </summary>
    /// <param name="horizontal">Horizontal radius.</param>
    /// <param name="vertical">Vertical radius.</param>
    /// <returns>The updated radii.</returns>
    public CssBorderRadius WithBottomRight(CssLength horizontal, CssLength vertical) =>
        new(TopLeftX, TopLeftY, TopRightX, TopRightY, horizontal, vertical, BottomLeftX, BottomLeftY);

    /// <summary>
    /// Returns a copy with a different bottom-left corner.
    /// </summary>
    /// <param name="horizontal">Horizontal radius.</param>
    /// <param name="vertical">Vertical radius.</param>
    /// <returns>The updated radii.</returns>
    public CssBorderRadius WithBottomLeft(CssLength horizontal, CssLength vertical) =>
        new(TopLeftX, TopLeftY, TopRightX, TopRightY, BottomRightX, BottomRightY, horizontal, vertical);

    /// <inheritdoc />
    public bool Equals(CssBorderRadius other) =>
        TopLeftX == other.TopLeftX && TopLeftY == other.TopLeftY &&
        TopRightX == other.TopRightX && TopRightY == other.TopRightY &&
        BottomRightX == other.BottomRightX && BottomRightY == other.BottomRightY &&
        BottomLeftX == other.BottomLeftX && BottomLeftY == other.BottomLeftY;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CssBorderRadius other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        HashCode.Combine(TopLeftX, TopLeftY, TopRightX, TopRightY),
        HashCode.Combine(BottomRightX, BottomRightY, BottomLeftX, BottomLeftY));

    /// <inheritdoc />
    public override string ToString() =>
        IsZero
            ? "0"
            : $"{TopLeftX} {TopRightX} {BottomRightX} {BottomLeftX} / {TopLeftY} {TopRightY} {BottomRightY} {BottomLeftY}";

    /// <summary>
    /// Expands a one-to-four value list into the four corners, clockwise from the top left.
    /// </summary>
    /// <param name="text">Whitespace separated list of one to four lengths.</param>
    /// <param name="corners">Corner values in top-left, top-right, bottom-right, bottom-left order.</param>
    /// <returns><c>true</c> when the list was parsed.</returns>
    private static bool TryParseCornerList(string? text, out CssLength[] corners)
    {
        corners = [];

        string[] tokens = CssValueParser.SplitValues(text);

        if (tokens.Length is < 1 or > 4)
        {
            return false;
        }

        var lengths = new CssLength[tokens.Length];

        for (int index = 0; index < tokens.Length; index++)
        {
            CssLength length = CssValueParser.ParseLength(tokens[index], CssLength.Auto);

            if (length.IsAuto)
            {
                return false;
            }

            lengths[index] = length;
        }

        corners = lengths.Length switch
        {
            1 => [lengths[0], lengths[0], lengths[0], lengths[0]],
            2 => [lengths[0], lengths[1], lengths[0], lengths[1]],
            3 => [lengths[0], lengths[1], lengths[2], lengths[1]],
            _ => [lengths[0], lengths[1], lengths[2], lengths[3]],
        };

        return true;
    }

    /// <summary>
    /// Determines whether a single radius is effectively zero.
    /// </summary>
    /// <param name="length">Radius to test.</param>
    /// <returns><c>true</c> for <c>auto</c> and for numeric zero.</returns>
    private static bool IsZeroLength(CssLength length) =>
        length.IsAuto || MathF.Abs(length.Value) < 0.0001f;
}
