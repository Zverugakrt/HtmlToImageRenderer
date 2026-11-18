using HtmlToImageRenderer.Parsing;

namespace HtmlToImageRenderer.Css;

/// <summary>
/// Resolves declarations into a <see cref="ComputedStyle"/> per element. The cascade applies
/// the user-agent sheet first, then the author sheet ordered by specificity and source order,
/// and finally the <c>style</c> attribute. Inherited properties are copied from the parent,
/// and results are cached so layout can query styles freely.
/// </summary>
public sealed class StyleResolver
{
    private readonly CssStylesheet[] _sheets;
    private readonly HtmlRendererOptions _options;
    private readonly CssParser _parser = new();
    private readonly Dictionary<HtmlElement, ComputedStyle> _cache = new();
    private readonly ComputedStyle _initialStyle;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleResolver"/> class.
    /// </summary>
    /// <param name="userAgentStylesheet">
    /// Lowest-priority sheet, normally <see cref="UserAgentStylesheet.Css"/> parsed by <see cref="CssParser"/>.
    /// </param>
    /// <param name="authorStylesheet">Sheet collected from the document itself.</param>
    /// <param name="options">Renderer options that provide the default font settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    public StyleResolver(CssStylesheet? userAgentStylesheet, CssStylesheet? authorStylesheet, HtmlRendererOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _sheets = [userAgentStylesheet ?? CssStylesheet.Empty, authorStylesheet ?? CssStylesheet.Empty];
        _initialStyle = CreateInitialStyle();
    }

    /// <summary>
    /// Gets the style used for elements without a parent, and as the seed for inheritance.
    /// </summary>
    public ComputedStyle InitialStyle => _initialStyle;

    /// <summary>
    /// Gets the root font size, which defines the value of <c>1rem</c>.
    /// </summary>
    public float RootFontSize => _options.DefaultFontSize;

    /// <summary>
    /// Resolves the computed style of an element, reusing cached results for its ancestors.
    /// </summary>
    /// <param name="element">Element to resolve; <c>null</c> yields <see cref="InitialStyle"/>.</param>
    /// <returns>The computed style of the element.</returns>
    public ComputedStyle Resolve(HtmlElement? element)
    {
        if (element is null)
        {
            return _initialStyle;
        }

        if (_cache.TryGetValue(element, out ComputedStyle cached))
        {
            return cached;
        }

        ComputedStyle parentStyle = element.Parent is { } parent ? Resolve(parent) : _initialStyle;
        ComputedStyle style = CreateInheritedStyle(parentStyle);
        ApplyDeclarations(style, element, parentStyle.FontSize);
        _cache[element] = style;
        return style;
    }

    /// <summary>
    /// Drops cached styles, for example after the document was mutated.
    /// </summary>
    public void ClearCache() => _cache.Clear();

    /// <summary>
    /// Builds the style every element starts from: initial values overridden by the
    /// renderer options.
    /// </summary>
    /// <returns>A fresh initial style.</returns>
    private ComputedStyle CreateInitialStyle() => new()
    {
        Display = DisplayMode.Inline,
        Color = CssColor.Black,
        BackgroundColor = CssColor.Transparent,
        FontFamily = _options.DefaultFontFamily,
        FontSize = _options.DefaultFontSize,
        FontWeight = 400,
        FontStyle = FontStyleKind.Normal,
        LineHeightMultiplier = _options.DefaultLineHeight,
        LineHeightPx = null,
        TextAlign = TextAlign.Left,
        WhiteSpace = WhiteSpaceKind.Normal,
        BorderTopStyle = BorderStyleKind.None,
        BorderRightStyle = BorderStyleKind.None,
        BorderBottomStyle = BorderStyleKind.None,
        BorderLeftStyle = BorderStyleKind.None,
        BorderTopColor = CssColor.Black,
        BorderRightColor = CssColor.Black,
        BorderBottomColor = CssColor.Black,
        BorderLeftColor = CssColor.Black,
        BorderRadius = CssBorderRadius.None,
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.NoWrap,
        JustifyContent = JustifyContent.FlexStart,
        AlignItems = AlignItems.Stretch,
        FlexGrow = 0f,
        FlexShrink = 1f,
        FlexBasis = CssLength.Auto,
    };

    /// <summary>
    /// Creates the starting style for a child element by copying the inherited properties
    /// from its parent and resetting everything else to the initial value.
    /// </summary>
    /// <param name="parentStyle">Computed style of the parent element.</param>
    /// <returns>A new style carrying the inherited values.</returns>
    private ComputedStyle CreateInheritedStyle(ComputedStyle parentStyle)
    {
        ComputedStyle style = CreateInitialStyle();
        style.Color = parentStyle.Color;
        style.FontFamily = parentStyle.FontFamily;
        style.FontSize = parentStyle.FontSize;
        style.FontWeight = parentStyle.FontWeight;
        style.FontStyle = parentStyle.FontStyle;
        style.LineHeightMultiplier = parentStyle.LineHeightMultiplier;
        style.LineHeightPx = parentStyle.LineHeightPx;
        style.TextAlign = parentStyle.TextAlign;
        style.WhiteSpace = parentStyle.WhiteSpace;
        return style;
    }

    /// <summary>
    /// Collects the declarations that match the element, orders them by the cascade and
    /// applies them in ascending priority.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="element">Element being resolved.</param>
    /// <param name="parentFontSize">Font size of the parent, needed by relative <c>font-size</c> values.</param>
    private void ApplyDeclarations(ComputedStyle style, HtmlElement element, float parentFontSize)
    {
        // Presentational attributes sit below every author rule, so they are applied first and any
        // CSS declaration, including the style attribute, simply overrides them.
        ApplyPresentationalHints(style, element);

        var matches = new List<MatchedDeclaration>();

        for (int sheetIndex = 0; sheetIndex < _sheets.Length; sheetIndex++)
        {
            foreach (CssRule rule in _sheets[sheetIndex].Rules)
            {
                bool matched = false;
                CssSpecificity bestSpecificity = CssSpecificity.Zero;

                foreach (CssSelector selector in rule.Selectors)
                {
                    if (!selector.Matches(element))
                    {
                        continue;
                    }

                    // A rule is applied once even when several of its selectors match;
                    // the highest matching specificity determines its cascade position.
                    if (!matched || selector.Specificity.CompareTo(bestSpecificity) > 0)
                    {
                        bestSpecificity = selector.Specificity;
                    }

                    matched = true;
                }

                if (!matched)
                {
                    continue;
                }

                foreach (CssDeclaration declaration in rule.Declarations)
                {
                    matches.Add(new MatchedDeclaration(bestSpecificity, sheetIndex, rule.Order, declaration));
                }
            }
        }

        matches.Sort(static (left, right) =>
        {
            int result = left.Specificity.CompareTo(right.Specificity);
            if (result != 0)
            {
                return result;
            }

            result = left.SheetIndex.CompareTo(right.SheetIndex);
            return result != 0 ? result : left.Order.CompareTo(right.Order);
        });

        foreach (MatchedDeclaration match in matches)
        {
            ApplyDeclaration(style, match.Declaration, parentFontSize);
        }

        // The style attribute is the last word, so it is applied after the sheets.
        string? inlineStyle = element.GetAttribute("style");
        if (!string.IsNullOrWhiteSpace(inlineStyle))
        {
            foreach (CssDeclaration declaration in _parser.ParseDeclarations(inlineStyle))
            {
                ApplyDeclaration(style, declaration, parentFontSize);
            }
        }
    }

    /// <summary>
    /// Applies one declaration to a style, ignoring unsupported properties and invalid values.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="declaration">Declaration to apply.</param>
    /// <param name="parentFontSize">Font size of the parent element.</param>
    private void ApplyDeclaration(ComputedStyle style, CssDeclaration declaration, float parentFontSize)
    {
        string value = declaration.Value.Trim();

        switch (declaration.Property)
        {
            case "display":
                ApplyDisplay(style, value);
                break;

            case "width":
                style.Width = CssValueParser.ParseLength(value, style.Width);
                break;

            case "height":
                style.Height = CssValueParser.ParseLength(value, style.Height);
                break;

            case "min-width":
                style.MinWidth = CssValueParser.ParseLength(value, style.MinWidth);
                break;

            case "min-height":
                style.MinHeight = CssValueParser.ParseLength(value, style.MinHeight);
                break;

            case "max-width":
                style.MaxWidth = CssValueParser.ParseLength(value, style.MaxWidth);
                break;

            case "max-height":
                style.MaxHeight = CssValueParser.ParseLength(value, style.MaxHeight);
                break;

            case "margin":
                if (CssValueParser.TryParseShorthand(value, out CssLength marginTop, out CssLength marginRight, out CssLength marginBottom, out CssLength marginLeft))
                {
                    style.MarginTop = marginTop;
                    style.MarginRight = marginRight;
                    style.MarginBottom = marginBottom;
                    style.MarginLeft = marginLeft;
                }

                break;

            case "margin-top":
                style.MarginTop = CssValueParser.ParseLength(value, style.MarginTop);
                break;

            case "margin-right":
                style.MarginRight = CssValueParser.ParseLength(value, style.MarginRight);
                break;

            case "margin-bottom":
                style.MarginBottom = CssValueParser.ParseLength(value, style.MarginBottom);
                break;

            case "margin-left":
                style.MarginLeft = CssValueParser.ParseLength(value, style.MarginLeft);
                break;

            case "padding":
                if (CssValueParser.TryParseShorthand(value, out CssLength paddingTop, out CssLength paddingRight, out CssLength paddingBottom, out CssLength paddingLeft))
                {
                    style.PaddingTop = paddingTop;
                    style.PaddingRight = paddingRight;
                    style.PaddingBottom = paddingBottom;
                    style.PaddingLeft = paddingLeft;
                }

                break;

            case "padding-top":
                style.PaddingTop = CssValueParser.ParseLength(value, style.PaddingTop);
                break;

            case "padding-right":
                style.PaddingRight = CssValueParser.ParseLength(value, style.PaddingRight);
                break;

            case "padding-bottom":
                style.PaddingBottom = CssValueParser.ParseLength(value, style.PaddingBottom);
                break;

            case "padding-left":
                style.PaddingLeft = CssValueParser.ParseLength(value, style.PaddingLeft);
                break;

            case "border":
                ApplyBorderToSides(style, BorderSides.All, value);
                break;

            case "border-top":
                ApplyBorderToSides(style, BorderSides.Top, value);
                break;

            case "border-right":
                ApplyBorderToSides(style, BorderSides.Right, value);
                break;

            case "border-bottom":
                ApplyBorderToSides(style, BorderSides.Bottom, value);
                break;

            case "border-left":
                ApplyBorderToSides(style, BorderSides.Left, value);
                break;

            case "border-width":
                ApplyBorderWidths(style, value);
                break;

            case "border-top-width":
                style.BorderTopWidth = ResolveAbsoluteLength(style, value, style.BorderTopWidth);
                break;

            case "border-right-width":
                style.BorderRightWidth = ResolveAbsoluteLength(style, value, style.BorderRightWidth);
                break;

            case "border-bottom-width":
                style.BorderBottomWidth = ResolveAbsoluteLength(style, value, style.BorderBottomWidth);
                break;

            case "border-left-width":
                style.BorderLeftWidth = ResolveAbsoluteLength(style, value, style.BorderLeftWidth);
                break;

            case "border-style":
                ApplyBorderStyles(style, value);
                break;

            case "border-top-style":
                style.BorderTopStyle = ParseBorderStyle(value, style.BorderTopStyle);
                break;

            case "border-right-style":
                style.BorderRightStyle = ParseBorderStyle(value, style.BorderRightStyle);
                break;

            case "border-bottom-style":
                style.BorderBottomStyle = ParseBorderStyle(value, style.BorderBottomStyle);
                break;

            case "border-left-style":
                style.BorderLeftStyle = ParseBorderStyle(value, style.BorderLeftStyle);
                break;

            case "border-color":
                ApplyBorderColors(style, value);
                break;

            case "border-top-color":
                if (CssColor.TryParse(value, out CssColor topBorderColor))
                {
                    style.BorderTopColor = topBorderColor;
                }

                break;

            case "border-right-color":
                if (CssColor.TryParse(value, out CssColor rightBorderColor))
                {
                    style.BorderRightColor = rightBorderColor;
                }

                break;

            case "border-bottom-color":
                if (CssColor.TryParse(value, out CssColor bottomBorderColor))
                {
                    style.BorderBottomColor = bottomBorderColor;
                }

                break;

            case "border-left-color":
                if (CssColor.TryParse(value, out CssColor leftBorderColor))
                {
                    style.BorderLeftColor = leftBorderColor;
                }

                break;

            case "border-radius":
                if (CssBorderRadius.TryParse(value, out CssBorderRadius borderRadius))
                {
                    style.BorderRadius = borderRadius;
                }

                break;

            case "border-top-left-radius":
                if (CssBorderRadius.TryParseCorner(value, out CssLength topLeftX, out CssLength topLeftY))
                {
                    style.BorderRadius = style.BorderRadius.WithTopLeft(topLeftX, topLeftY);
                }

                break;

            case "border-top-right-radius":
                if (CssBorderRadius.TryParseCorner(value, out CssLength topRightX, out CssLength topRightY))
                {
                    style.BorderRadius = style.BorderRadius.WithTopRight(topRightX, topRightY);
                }

                break;

            case "border-bottom-right-radius":
                if (CssBorderRadius.TryParseCorner(value, out CssLength bottomRightX, out CssLength bottomRightY))
                {
                    style.BorderRadius = style.BorderRadius.WithBottomRight(bottomRightX, bottomRightY);
                }

                break;

            case "border-bottom-left-radius":
                if (CssBorderRadius.TryParseCorner(value, out CssLength bottomLeftX, out CssLength bottomLeftY))
                {
                    style.BorderRadius = style.BorderRadius.WithBottomLeft(bottomLeftX, bottomLeftY);
                }

                break;

            case "box-sizing":
                style.UseBorderBox = string.Equals(value, "border-box", StringComparison.OrdinalIgnoreCase);
                break;

            case "background":
            case "background-color":
                ApplyBackground(style, value);
                break;

            case "color":
                if (CssColor.TryParse(value, out CssColor textColor))
                {
                    style.Color = textColor;
                }

                break;

            case "font-family":
                string? family = CssValueParser.ParseFirstFontFamily(value);
                if (!string.IsNullOrEmpty(family))
                {
                    style.FontFamily = family;
                }

                break;

            case "font-size":
                style.FontSize = CssValueParser.ParseFontSize(value, parentFontSize, _options.DefaultFontSize, style.FontSize);
                break;

            case "font-weight":
                ApplyFontWeight(style, value);
                break;

            case "font-style":
                style.FontStyle = value.Contains("italic", StringComparison.OrdinalIgnoreCase) ||
                                  value.Contains("oblique", StringComparison.OrdinalIgnoreCase)
                    ? FontStyleKind.Italic
                    : FontStyleKind.Normal;
                break;

            case "font":
                ApplyFontShorthand(style, value, parentFontSize);
                break;

            case "line-height":
                ApplyLineHeight(style, value);
                break;

            case "text-align":
                style.TextAlign = value.ToLowerInvariant() switch
                {
                    "center" => TextAlign.Center,
                    "right" or "end" => TextAlign.Right,
                    "justify" => TextAlign.Justify,
                    _ => TextAlign.Left,
                };
                break;

            case "white-space":
                style.WhiteSpace = value.ToLowerInvariant() switch
                {
                    "nowrap" => WhiteSpaceKind.NoWrap,
                    "pre" => WhiteSpaceKind.Pre,
                    "pre-wrap" or "pre-line" or "break-spaces" => WhiteSpaceKind.PreWrap,
                    _ => WhiteSpaceKind.Normal,
                };
                break;

            case "text-decoration":
            case "text-decoration-line":
                style.TextDecorationUnderline = value.Contains("underline", StringComparison.OrdinalIgnoreCase);
                break;

            case "flex-direction":
                style.FlexDirection = value.ToLowerInvariant() switch
                {
                    "row-reverse" => FlexDirection.RowReverse,
                    "column" => FlexDirection.Column,
                    "column-reverse" => FlexDirection.ColumnReverse,
                    _ => FlexDirection.Row,
                };
                break;

            case "flex-wrap":
                style.FlexWrap = value.ToLowerInvariant() switch
                {
                    "wrap" => FlexWrap.Wrap,
                    "wrap-reverse" => FlexWrap.WrapReverse,
                    _ => FlexWrap.NoWrap,
                };
                break;

            case "justify-content":
                style.JustifyContent = value.ToLowerInvariant() switch
                {
                    "flex-end" or "end" or "right" => JustifyContent.FlexEnd,
                    "center" => JustifyContent.Center,
                    "space-between" => JustifyContent.SpaceBetween,
                    "space-around" => JustifyContent.SpaceAround,
                    "space-evenly" => JustifyContent.SpaceEvenly,
                    _ => JustifyContent.FlexStart,
                };
                break;

            case "align-items":
                style.AlignItems = ParseAlignItems(value, AlignItems.Stretch);
                break;

            case "align-self":
                // "auto" defers to the container, which is expressed as null here.
                style.AlignSelf = string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : ParseAlignItems(value, AlignItems.Stretch);
                break;

            case "gap":
                if (CssValueParser.TryParseShorthand(value, out CssLength rowGap, out CssLength columnGap, out _, out _))
                {
                    style.RowGap = ResolveAbsolute(style, rowGap, style.RowGap);
                    style.ColumnGap = ResolveAbsolute(style, columnGap, style.ColumnGap);
                }

                break;

            case "row-gap":
                style.RowGap = ResolveAbsoluteLength(style, value, style.RowGap);
                break;

            case "column-gap":
                style.ColumnGap = ResolveAbsoluteLength(style, value, style.ColumnGap);
                break;

            case "flex-grow":
                if (CssValueParser.TryParseNumber(value, out float grow))
                {
                    style.FlexGrow = MathF.Max(grow, 0f);
                }

                break;

            case "flex-shrink":
                if (CssValueParser.TryParseNumber(value, out float shrink))
                {
                    style.FlexShrink = MathF.Max(shrink, 0f);
                }

                break;

            case "flex-basis":
                style.FlexBasis = CssValueParser.ParseLength(value, style.FlexBasis);
                break;

            case "flex":
                ApplyFlexShorthand(style, value);
                break;

            default:
                // Unsupported properties (position, opacity, background-image, order, ...) are ignored.
                break;
        }
    }

    /// <summary>
    /// Applies legacy presentational attributes that predate CSS. They behave like author level
    /// hints with the lowest priority, so every CSS rule overrides them.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="element">Element that may carry the attributes.</param>
    private static void ApplyPresentationalHints(ComputedStyle style, HtmlElement element)
    {
        string? align = element.GetAttribute("align");

        if (string.IsNullOrWhiteSpace(align))
        {
            return;
        }

        string value = align.Trim().ToLowerInvariant();

        // The same attribute means two different things: on a table it places the box inside its
        // container, on a row or a cell it sets the horizontal alignment of the text.
        if (string.Equals(element.TagName, "table", StringComparison.Ordinal))
        {
            switch (value)
            {
                case "center" or "middle":
                    // The attribute also sizes the table to its content, otherwise auto margins would
                    // have no free space and the table would still look left aligned.
                    style.ShrinkToFitWidth = true;
                    style.MarginLeft = CssLength.Auto;
                    style.MarginRight = CssLength.Auto;
                    break;

                case "right":
                    style.ShrinkToFitWidth = true;
                    style.MarginLeft = CssLength.Auto;
                    style.MarginRight = CssLength.Zero;
                    break;

                case "left":
                    style.ShrinkToFitWidth = true;
                    break;
            }

            return;
        }

        if (element.TagName is not ("td" or "th" or "tr"))
        {
            return;
        }

        // text-align is inherited, so a value set on a row reaches its cells without extra work.
        TextAlign? textAlign = value switch
        {
            "left" or "start" => TextAlign.Left,
            "center" or "middle" => TextAlign.Center,
            "right" or "end" => TextAlign.Right,
            "justify" => TextAlign.Justify,
            _ => null,
        };

        if (textAlign.HasValue)
        {
            style.TextAlign = textAlign.Value;
        }
    }

    /// <summary>
    /// Maps a <c>display</c> value onto the supported box types.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw property value.</param>
    private static void ApplyDisplay(ComputedStyle style, string value)
    {
        style.Display = value.ToLowerInvariant() switch
        {
            "none" => DisplayMode.None,
            "flex" or "inline-flex" => DisplayMode.Flex,
            "table" or "inline-table" => DisplayMode.Table,
            "table-row" => DisplayMode.TableRow,
            "table-cell" => DisplayMode.TableCell,
            "table-row-group" or "table-header-group" or "table-footer-group" => DisplayMode.TableRowGroup,
            "table-caption" => DisplayMode.Block,
            "inline-block" => DisplayMode.InlineBlock,
            "inline" or "contents" => DisplayMode.Inline,
            // block, list-item, grid and anything unknown degrade to block layout.
            _ => DisplayMode.Block,
        };
    }

    /// <summary>
    /// Maps a <c>font-weight</c> value onto a numeric weight.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw property value.</param>
    private static void ApplyFontWeight(ComputedStyle style, string value)
    {
        switch (value.ToLowerInvariant())
        {
            case "normal":
                style.FontWeight = 400;
                return;
            case "bold":
                style.FontWeight = 700;
                return;
            case "bolder":
                style.FontWeight = Math.Min(900, style.FontWeight + 300);
                return;
            case "lighter":
                style.FontWeight = Math.Max(100, style.FontWeight - 300);
                return;
        }

        if (int.TryParse(value, out int weight))
        {
            style.FontWeight = Math.Clamp(weight, 100, 900);
        }
    }

    /// <summary>
    /// Applies <c>line-height</c>, which is either a unitless multiplier or an absolute length.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw property value.</param>
    private void ApplyLineHeight(ComputedStyle style, string value)
    {
        string normalized = value.Trim().ToLowerInvariant();

        if (normalized == "normal")
        {
            style.LineHeightPx = null;
            style.LineHeightMultiplier = _options.DefaultLineHeight;
            return;
        }

        if (normalized.EndsWith('%'))
        {
            if (CssValueParser.TryParseNumber(normalized[..^1], out float percent))
            {
                style.LineHeightPx = percent / 100f * style.FontSize;
            }

            return;
        }

        if (CssValueParser.TryParseNumber(normalized, out float multiplier))
        {
            style.LineHeightPx = null;
            style.LineHeightMultiplier = MathF.Max(multiplier, 0.1f);
            return;
        }

        CssLength length = CssValueParser.ParseLength(normalized, CssLength.Auto);
        if (!length.IsAuto)
        {
            style.LineHeightPx = ResolveAbsolute(style, length, style.ResolveLineHeight());
        }
    }

    /// <summary>
    /// Applies a border shorthand to the requested sides. The value may combine width, style and
    /// color in any order, and each part only overwrites what it actually specifies, which is what
    /// makes <c>border: 1px</c> keep the style and color of an earlier declaration.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="sides">Sides the shorthand applies to.</param>
    /// <param name="value">Raw shorthand value.</param>
    private void ApplyBorderToSides(ComputedStyle style, BorderSides sides, string value)
    {
        BorderStyleKind? parsedStyle = null;
        CssColor? parsedColor = null;
        float? parsedWidth = null;

        foreach (string token in CssValueParser.SplitValues(value))
        {
            string normalized = token.ToLowerInvariant();

            if (IsBorderStyleKeyword(normalized))
            {
                parsedStyle = ParseBorderStyle(normalized, BorderStyleKind.None);
                continue;
            }

            if (CssColor.TryParse(normalized, out CssColor color))
            {
                parsedColor = color;
                continue;
            }

            CssLength length = CssValueParser.ParseLength(normalized, CssLength.Auto);
            if (!length.IsAuto)
            {
                parsedWidth = ResolveAbsolute(style, length, 0f);
            }
        }

        ApplyBorderParts(style, sides, parsedWidth, parsedStyle, parsedColor);
    }

    /// <summary>
    /// Writes the parsed parts of a border shorthand onto the requested sides.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="sides">Sides that receive the values.</param>
    /// <param name="width">Parsed width, or <c>null</c> when the shorthand did not set one.</param>
    /// <param name="borderStyle">Parsed line style, or <c>null</c> when the shorthand did not set one.</param>
    /// <param name="color">Parsed color, or <c>null</c> when the shorthand did not set one.</param>
    private static void ApplyBorderParts(
        ComputedStyle style,
        BorderSides sides,
        float? width,
        BorderStyleKind? borderStyle,
        CssColor? color)
    {
        if (width.HasValue)
        {
            if (sides.HasFlag(BorderSides.Top))
            {
                style.BorderTopWidth = width.Value;
            }

            if (sides.HasFlag(BorderSides.Right))
            {
                style.BorderRightWidth = width.Value;
            }

            if (sides.HasFlag(BorderSides.Bottom))
            {
                style.BorderBottomWidth = width.Value;
            }

            if (sides.HasFlag(BorderSides.Left))
            {
                style.BorderLeftWidth = width.Value;
            }
        }

        if (borderStyle.HasValue)
        {
            if (sides.HasFlag(BorderSides.Top))
            {
                style.BorderTopStyle = borderStyle.Value;
            }

            if (sides.HasFlag(BorderSides.Right))
            {
                style.BorderRightStyle = borderStyle.Value;
            }

            if (sides.HasFlag(BorderSides.Bottom))
            {
                style.BorderBottomStyle = borderStyle.Value;
            }

            if (sides.HasFlag(BorderSides.Left))
            {
                style.BorderLeftStyle = borderStyle.Value;
            }
        }

        if (color.HasValue)
        {
            if (sides.HasFlag(BorderSides.Top))
            {
                style.BorderTopColor = color.Value;
            }

            if (sides.HasFlag(BorderSides.Right))
            {
                style.BorderRightColor = color.Value;
            }

            if (sides.HasFlag(BorderSides.Bottom))
            {
                style.BorderBottomColor = color.Value;
            }

            if (sides.HasFlag(BorderSides.Left))
            {
                style.BorderLeftColor = color.Value;
            }
        }
    }

    /// <summary>
    /// Applies the <c>border-style</c> shorthand, which accepts one to four values distributed
    /// clockwise from the top side.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw property value.</param>
    private static void ApplyBorderStyles(ComputedStyle style, string value)
    {
        string[]? sides = ExpandFour(value);

        if (sides is null)
        {
            return;
        }

        style.BorderTopStyle = ParseBorderStyle(sides[0], style.BorderTopStyle);
        style.BorderRightStyle = ParseBorderStyle(sides[1], style.BorderRightStyle);
        style.BorderBottomStyle = ParseBorderStyle(sides[2], style.BorderBottomStyle);
        style.BorderLeftStyle = ParseBorderStyle(sides[3], style.BorderLeftStyle);
    }

    /// <summary>
    /// Applies the <c>border-color</c> shorthand, which accepts one to four values distributed
    /// clockwise from the top side.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw property value.</param>
    private static void ApplyBorderColors(ComputedStyle style, string value)
    {
        string[]? sides = ExpandFour(value);

        if (sides is null)
        {
            return;
        }

        if (CssColor.TryParse(sides[0], out CssColor top))
        {
            style.BorderTopColor = top;
        }

        if (CssColor.TryParse(sides[1], out CssColor right))
        {
            style.BorderRightColor = right;
        }

        if (CssColor.TryParse(sides[2], out CssColor bottom))
        {
            style.BorderBottomColor = bottom;
        }

        if (CssColor.TryParse(sides[3], out CssColor left))
        {
            style.BorderLeftColor = left;
        }
    }

    /// <summary>
    /// Expands a one-to-four value list into exactly four values ordered top, right, bottom, left.
    /// </summary>
    /// <param name="value">Whitespace separated list of one to four values.</param>
    /// <returns>The four values, or <c>null</c> when the list has an unsupported length.</returns>
    private static string[]? ExpandFour(string value)
    {
        string[] tokens = CssValueParser.SplitValues(value);

        return tokens.Length switch
        {
            1 => [tokens[0], tokens[0], tokens[0], tokens[0]],
            2 => [tokens[0], tokens[1], tokens[0], tokens[1]],
            3 => [tokens[0], tokens[1], tokens[2], tokens[1]],
            4 => [tokens[0], tokens[1], tokens[2], tokens[3]],
            _ => null,
        };
    }

    /// <summary>
    /// Determines whether a token is a <c>border-style</c> keyword.
    /// </summary>
    /// <param name="value">Lower-case token.</param>
    /// <returns><c>true</c> for the styles this renderer understands.</returns>
    private static bool IsBorderStyleKeyword(string value) =>
        value is "none" or "hidden" or "solid" or "dashed" or "dotted"
            or "double" or "groove" or "ridge" or "inset" or "outset";

    /// <summary>
    /// Applies the one-to-four value <c>border-width</c> shorthand.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw shorthand value.</param>
    private void ApplyBorderWidths(ComputedStyle style, string value)
    {
        if (!CssValueParser.TryParseShorthand(value, out CssLength top, out CssLength right, out CssLength bottom, out CssLength left))
        {
            return;
        }

        style.BorderTopWidth = ResolveAbsolute(style, top, style.BorderTopWidth);
        style.BorderRightWidth = ResolveAbsolute(style, right, style.BorderRightWidth);
        style.BorderBottomWidth = ResolveAbsolute(style, bottom, style.BorderBottomWidth);
        style.BorderLeftWidth = ResolveAbsolute(style, left, style.BorderLeftWidth);
    }

    /// <summary>
    /// Applies <c>background</c> or <c>background-color</c>. Only the color component is
    /// supported; image and repeat values are ignored.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw property value.</param>
    private static void ApplyBackground(ComputedStyle style, string value)
    {
        if (CssColor.TryParse(value, out CssColor color))
        {
            style.BackgroundColor = color;
            return;
        }

        string[] tokens = CssValueParser.SplitValues(value);
        foreach (string token in tokens)
        {
            if (CssColor.TryParse(token, out color))
            {
                style.BackgroundColor = color;
                return;
            }
        }
    }

    /// <summary>
    /// Applies the <c>font</c> shorthand for the parts this renderer supports:
    /// style, weight, size and family.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw shorthand value.</param>
    /// <param name="parentFontSize">Font size of the parent element.</param>
    private void ApplyFontShorthand(ComputedStyle style, string value, float parentFontSize)
    {
        string[] tokens = CssValueParser.SplitValues(value);
        int sizeIndex = -1;

        for (int index = 0; index < tokens.Length; index++)
        {
            string token = tokens[index].ToLowerInvariant();

            switch (token)
            {
                case "italic" or "oblique":
                    style.FontStyle = FontStyleKind.Italic;
                    continue;
                case "bold":
                    style.FontWeight = 700;
                    continue;
                case "bolder":
                    style.FontWeight = Math.Min(900, style.FontWeight + 300);
                    continue;
                case "lighter":
                    style.FontWeight = Math.Max(100, style.FontWeight - 300);
                    continue;
                case "normal":
                    continue;
            }

            if (LooksLikeFontSize(token))
            {
                style.FontSize = CssValueParser.ParseFontSize(token, parentFontSize, _options.DefaultFontSize, style.FontSize);
                sizeIndex = index;
                break;
            }

            if (int.TryParse(token, out int weight) && weight is >= 100 and <= 900)
            {
                style.FontWeight = weight;
            }
        }

        if (sizeIndex >= 0 && sizeIndex + 1 < tokens.Length)
        {
            string remainder = string.Join(' ', tokens[(sizeIndex + 1)..]);
            string? family = CssValueParser.ParseFirstFontFamily(remainder);
            if (!string.IsNullOrEmpty(family))
            {
                style.FontFamily = family;
            }
        }
    }

    /// <summary>
    /// Applies the <c>flex</c> shorthand: grow, shrink and basis in any supported combination.
    /// </summary>
    /// <param name="style">Style being built.</param>
    /// <param name="value">Raw shorthand value.</param>
    private void ApplyFlexShorthand(ComputedStyle style, string value)
    {
        string normalized = value.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "none":
                style.FlexGrow = 0f;
                style.FlexShrink = 0f;
                style.FlexBasis = CssLength.Auto;
                return;
            case "auto":
                style.FlexGrow = 1f;
                style.FlexShrink = 1f;
                style.FlexBasis = CssLength.Auto;
                return;
            case "initial":
                style.FlexGrow = 0f;
                style.FlexShrink = 1f;
                style.FlexBasis = CssLength.Auto;
                return;
        }

        bool growSet = false;
        bool shrinkSet = false;

        foreach (string token in CssValueParser.SplitValues(value))
        {
            if (CssValueParser.TryParseNumber(token, out float number))
            {
                if (!growSet)
                {
                    style.FlexGrow = MathF.Max(number, 0f);
                    growSet = true;
                }
                else if (!shrinkSet)
                {
                    style.FlexShrink = MathF.Max(number, 0f);
                    shrinkSet = true;
                }

                continue;
            }

            CssLength basis = CssValueParser.ParseLength(token, CssLength.Auto);
            style.FlexBasis = basis;
        }

        // `flex: 1` means grow 1, shrink 1, basis 0, which is what authors expect.
        if (growSet && !shrinkSet && style.FlexBasis.IsAuto)
        {
            style.FlexShrink = 1f;
            style.FlexBasis = CssLength.Zero;
        }
    }

    /// <summary>
    /// Parses a border style keyword, falling back to the supplied value.
    /// </summary>
    /// <param name="value">Raw keyword.</param>
    /// <param name="fallback">Value used for unsupported keywords.</param>
    /// <returns>The parsed border style.</returns>
    private static BorderStyleKind ParseBorderStyle(string value, BorderStyleKind fallback) =>
        value.ToLowerInvariant() switch
        {
            "none" or "hidden" => BorderStyleKind.None,
            "solid" or "double" or "groove" or "ridge" or "inset" or "outset" => BorderStyleKind.Solid,
            "dashed" => BorderStyleKind.Dashed,
            "dotted" => BorderStyleKind.Dotted,
            _ => fallback,
        };

    /// <summary>
    /// Parses an <c>align-items</c> / <c>align-self</c> keyword.
    /// </summary>
    /// <param name="value">Raw keyword.</param>
    /// <param name="fallback">Value used for unsupported keywords.</param>
    /// <returns>The parsed alignment.</returns>
    private static AlignItems ParseAlignItems(string value, AlignItems fallback) =>
        value.ToLowerInvariant() switch
        {
            "flex-end" or "end" => AlignItems.FlexEnd,
            "center" => AlignItems.Center,
            "stretch" => AlignItems.Stretch,
            "baseline" => AlignItems.Baseline,
            "flex-start" or "start" => AlignItems.FlexStart,
            _ => fallback,
        };

    /// <summary>
    /// Resolves a length to pixels for properties that never accept percentages.
    /// </summary>
    /// <param name="style">Style that provides the font sizes.</param>
    /// <param name="length">Length to resolve.</param>
    /// <param name="fallback">Value used when the length is <c>auto</c>.</param>
    /// <returns>The pixel value.</returns>
    private float ResolveAbsolute(ComputedStyle style, CssLength length, float fallback)
    {
        if (length.IsAuto)
        {
            return fallback;
        }

        return length.Resolve(0f, style.FontSize, _options.DefaultFontSize);
    }

    /// <summary>
    /// Parses and resolves a single-token length for a border or gap property.
    /// </summary>
    /// <param name="style">Style that provides the font sizes.</param>
    /// <param name="value">Raw property value.</param>
    /// <param name="fallback">Value used when parsing fails.</param>
    /// <returns>The pixel value.</returns>
    private float ResolveAbsoluteLength(ComputedStyle style, string value, float fallback) =>
        ResolveAbsolute(style, CssValueParser.ParseLength(value, CssLength.Auto), fallback);

    /// <summary>
    /// Determines whether a token can only be a font size inside the <c>font</c> shorthand.
    /// </summary>
    /// <param name="token">Lower-case token.</param>
    /// <returns><c>true</c> for size keywords and length units.</returns>
    private static bool LooksLikeFontSize(string token) =>
        token is "xx-small" or "x-small" or "small" or "medium" or "large" or "x-large" or "xx-large"
            or "smaller" or "larger" ||
        token.EndsWith("px", StringComparison.Ordinal) ||
        token.EndsWith("pt", StringComparison.Ordinal) ||
        token.EndsWith("em", StringComparison.Ordinal) ||
        token.EndsWith("rem", StringComparison.Ordinal) ||
        token.EndsWith('%');

    /// <summary>
    /// One declaration together with its cascade position.
    /// </summary>
    /// <param name="Specificity">Specificity of the selector that matched.</param>
    /// <param name="SheetIndex">Position of the sheet; the user-agent sheet is <c>0</c>.</param>
    /// <param name="Order">Position of the rule inside its sheet.</param>
    /// <param name="Declaration">Declaration to apply.</param>
    private readonly record struct MatchedDeclaration(
        CssSpecificity Specificity,
        int SheetIndex,
        int Order,
        CssDeclaration Declaration);
}
