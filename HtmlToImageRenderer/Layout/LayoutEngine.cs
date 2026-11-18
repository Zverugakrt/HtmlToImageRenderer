using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Entry point of the layout stage. It wires the block, inline and flex engines together and
/// lays out the document root, producing a box tree that the painter can consume.
/// </summary>
public sealed class LayoutEngine
{
    /// <summary>
    /// Maximum nesting depth of boxes. It stops runaway recursion on pathological markup
    /// instead of overflowing the stack.
    /// </summary>
    public const int MaxDepth = 200;

    private readonly BlockLayoutEngine _block;
    private readonly FlexLayoutEngine _flex;
    private readonly TableLayoutEngine _table;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutEngine"/> class.
    /// </summary>
    /// <param name="context">Layout context for the render being processed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
    public LayoutEngine(LayoutContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _block = new BlockLayoutEngine(context);
        _flex = new FlexLayoutEngine(context, _block);
        _table = new TableLayoutEngine(context, _block);

        // The engines are mutually recursive: blocks may contain flex containers or tables, and
        // their items or cells are laid out as blocks again.
        _block.Flex = _flex;
        _block.Table = _table;
    }

    /// <summary>
    /// Lays out a document root. The returned box is positioned at its own margin offset so
    /// that a <c>body</c> margin of eight pixels indents the whole page.
    /// </summary>
    /// <param name="rootElement">Root element, usually the document body.</param>
    /// <returns>The root layout box.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rootElement"/> is <c>null</c>.</exception>
    public LayoutBox Layout(HtmlElement rootElement)
    {
        ArgumentNullException.ThrowIfNull(rootElement);

        LayoutBox box = _block.Layout(rootElement);
        box.X = box.Margin.Left;
        box.Y = box.Margin.Top;
        return box;
    }

    /// <summary>
    /// Gets the height the document occupies, margins included.
    /// </summary>
    /// <param name="root">Root box produced by <see cref="Layout"/>.</param>
    /// <returns>The document height in CSS pixels.</returns>
    public static float GetDocumentHeight(LayoutBox root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.Margin.Top + root.Height + root.Margin.Bottom;
    }

    /// <summary>
    /// Gets the width the document occupies, margins included.
    /// </summary>
    /// <param name="root">Root box produced by <see cref="Layout"/>.</param>
    /// <returns>The document width in CSS pixels.</returns>
    public static float GetDocumentWidth(LayoutBox root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.Margin.Left + root.Width + root.Margin.Right;
    }
}
