namespace HtmlToImageRenderer.Css;

/// <summary>
/// CSS selector specificity, ordered the way the cascade uses it: identifiers outweigh
/// classes, classes outweigh element names.
/// </summary>
public readonly struct CssSpecificity : IComparable<CssSpecificity>, IEquatable<CssSpecificity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssSpecificity"/> struct.
    /// </summary>
    /// <param name="idCount">Number of <c>#id</c> parts in the selector.</param>
    /// <param name="classCount">Number of class, attribute and pseudo-class parts.</param>
    /// <param name="typeCount">Number of element names and pseudo-elements.</param>
    public CssSpecificity(int idCount, int classCount, int typeCount)
    {
        IdCount = idCount;
        ClassCount = classCount;
        TypeCount = typeCount;
    }

    /// <summary>
    /// Gets the number of <c>#id</c> parts.
    /// </summary>
    public int IdCount { get; }

    /// <summary>
    /// Gets the number of class parts.
    /// </summary>
    public int ClassCount { get; }

    /// <summary>
    /// Gets the number of element-name parts.
    /// </summary>
    public int TypeCount { get; }

    /// <summary>
    /// Gets the specificity of the universal selector.
    /// </summary>
    public static CssSpecificity Zero => new(0, 0, 0);

    /// <summary>
    /// Returns a new specificity increased by another one.
    /// </summary>
    /// <param name="other">Specificity to add.</param>
    /// <returns>The combined specificity.</returns>
    public CssSpecificity Add(CssSpecificity other) => new(
        IdCount + other.IdCount,
        ClassCount + other.ClassCount,
        TypeCount + other.TypeCount);

    /// <inheritdoc />
    public int CompareTo(CssSpecificity other)
    {
        int result = IdCount.CompareTo(other.IdCount);
        if (result != 0)
        {
            return result;
        }

        result = ClassCount.CompareTo(other.ClassCount);
        return result != 0 ? result : TypeCount.CompareTo(other.TypeCount);
    }

    /// <inheritdoc />
    public bool Equals(CssSpecificity other) =>
        IdCount == other.IdCount && ClassCount == other.ClassCount && TypeCount == other.TypeCount;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CssSpecificity other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(IdCount, ClassCount, TypeCount);

    /// <inheritdoc />
    public override string ToString() => $"({IdCount},{ClassCount},{TypeCount})";
}
