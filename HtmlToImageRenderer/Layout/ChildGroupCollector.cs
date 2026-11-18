using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Splits the children of a block or flex container into block-level groups and anonymous
/// runs of inline content. Consecutive inline-level nodes (text and inline elements) belong
/// to the same anonymous group, which is what makes
/// <c>&lt;div&gt;text&lt;span&gt;more&lt;/span&gt;&lt;p&gt;block&lt;/p&gt;&lt;/div&gt;</c>
/// produce two line box groups around a block child.
/// </summary>
internal static class ChildGroupCollector
{
    /// <summary>
    /// Groups the children of an element.
    /// </summary>
    /// <param name="element">Container whose children are grouped.</param>
    /// <param name="styles">Resolver used to determine each child's display type.</param>
    /// <returns>The groups in document order; children with <c>display: none</c> are skipped.</returns>
    public static List<InlineGroup> Collect(HtmlElement element, StyleResolver styles)
    {
        var groups = new List<InlineGroup>();
        InlineGroup? pendingInline = null;

        void FlushInline()
        {
            if (pendingInline is not null && pendingInline.Nodes.Count > 0)
            {
                groups.Add(pendingInline);
            }

            pendingInline = null;
        }

        foreach (HtmlNode child in element.Children)
        {
            switch (child)
            {
                case HtmlTextNode textNode:
                    if (textNode.Text.Length == 0)
                    {
                        break;
                    }

                    pendingInline ??= new InlineGroup();
                    pendingInline.Nodes.Add(textNode);
                    break;

                case HtmlElement childElement:
                {
                    ComputedStyle style = styles.Resolve(childElement);
                    if (style.Display == DisplayMode.None)
                    {
                        break;
                    }

                    if (style.IsInlineLevel)
                    {
                        pendingInline ??= new InlineGroup();
                        pendingInline.Nodes.Add(childElement);
                        break;
                    }

                    FlushInline();
                    groups.Add(new InlineGroup(childElement) { Style = style });
                    break;
                }
            }
        }

        FlushInline();
        return groups;
    }
}
