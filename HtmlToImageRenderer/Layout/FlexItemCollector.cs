using HtmlToImageRenderer.Css;
using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Collects the children of a flex container into flex items. Unlike block flow, every child
/// element becomes its own item regardless of its <c>display</c> value, because CSS makes every
/// in-flow child of a flex container a flex item; only runs of text nodes produce anonymous items.
/// </summary>
internal static class FlexItemCollector
{
    /// <summary>
    /// Groups the children of a flex container into flex items.
    /// </summary>
    /// <param name="element">Flex container whose children are collected.</param>
    /// <param name="styles">Resolver used to determine each child's display type.</param>
    /// <returns>
    /// The items in document order. Children with <c>display: none</c> and white-space-only text
    /// runs are skipped, since they generate no box at all.
    /// </returns>
    public static List<InlineGroup> Collect(HtmlElement element, StyleResolver styles)
    {
        var items = new List<InlineGroup>();
        List<HtmlNode>? pendingText = null;

        void FlushText()
        {
            if (pendingText is null)
            {
                return;
            }

            var group = new InlineGroup();
            group.Nodes.AddRange(pendingText);
            items.Add(group);
            pendingText = null;
        }

        foreach (HtmlNode child in element.Children)
        {
            switch (child)
            {
                case HtmlTextNode textNode:
                    // White space between items is never rendered by a flex container.
                    if (string.IsNullOrWhiteSpace(textNode.Text))
                    {
                        break;
                    }

                    pendingText ??= new List<HtmlNode>();
                    pendingText.Add(textNode);
                    break;

                case HtmlElement childElement:
                {
                    FlushText();

                    ComputedStyle style = styles.Resolve(childElement);
                    if (style.Display == DisplayMode.None)
                    {
                        break;
                    }

                    items.Add(new InlineGroup(childElement) { Style = style });
                    break;
                }
            }
        }

        FlushText();
        return items;
    }
}
