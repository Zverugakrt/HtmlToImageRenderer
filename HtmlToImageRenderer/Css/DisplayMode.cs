namespace HtmlToImageRenderer.Css;

/// <summary>
/// Value of the <c>display</c> property. Properties not listed here collapse to
/// the closest supported behavior: <c>inline-block</c> is treated as an inline-level
/// box with its own block formatting context and <c>grid</c> falls back to <c>block</c>.
/// </summary>
public enum DisplayMode
{
    /// <summary>Block-level box that fills the containing block width.</summary>
    Block,

    /// <summary>Inline-level box that participates in a line box.</summary>
    Inline,

    /// <summary>Inline-level box that lays out its children as a block.</summary>
    InlineBlock,

    /// <summary>Flex container; children are laid out along the main axis.</summary>
    Flex,

    /// <summary>Table container; rows and cells are laid out on a shared column grid.</summary>
    Table,

    /// <summary>
    /// Group of table rows such as <c>thead</c>, <c>tbody</c> or <c>tfoot</c>.
    /// The box is transparent for column computation: its rows belong to the table grid.
    /// </summary>
    TableRowGroup,

    /// <summary>Table row; its cells share the column widths of the table.</summary>
    TableRow,

    /// <summary>Table cell; it holds block content and participates in the column grid.</summary>
    TableCell,

    /// <summary>Element generates no box at all.</summary>
    None,
}
