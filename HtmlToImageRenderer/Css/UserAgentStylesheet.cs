namespace HtmlToImageRenderer.Css;

/// <summary>
/// Lowest-priority stylesheet that gives the standard HTML elements their familiar
/// default appearance: block-level containers, bold headings, list indentation,
/// emphasis and link styling. Author rules override it through normal specificity,
/// and inline styles override both.
/// </summary>
public static class UserAgentStylesheet
{
    /// <summary>
    /// Gets the CSS source of the user-agent stylesheet.
    /// </summary>
    public const string Css = """
        html { display: block; }
        body { display: block; margin: 8px; }
        div, section, article, header, footer, main, nav, aside, figure, figcaption,
        form, fieldset, blockquote, dl, dt, dd, address, center, details, summary,
        ul, ol, li, pre, hr, p,
        h1, h2, h3, h4, h5, h6 { display: block; }

        table { display: table; }
        thead, tbody, tfoot { display: table-row-group; }
        tr { display: table-row; }
        td, th { display: table-cell; padding: 1px; }
        th { font-weight: bold; text-align: center; }
        caption { display: block; }

        p { margin: 1em 0; }
        h1 { font-size: 2em; font-weight: bold; margin: 0.67em 0; }
        h2 { font-size: 1.5em; font-weight: bold; margin: 0.83em 0; }
        h3 { font-size: 1.17em; font-weight: bold; margin: 1em 0; }
        h4 { font-size: 1em; font-weight: bold; margin: 1.33em 0; }
        h5 { font-size: 0.83em; font-weight: bold; margin: 1.67em 0; }
        h6 { font-size: 0.67em; font-weight: bold; margin: 2.33em 0; }

        ul, ol { padding-left: 40px; margin: 1em 0; }
        ul ul, ul ol, ol ul, ol ol { margin: 0; }
        dl { margin: 1em 0; }
        dt { font-weight: bold; }
        dd { margin-left: 40px; }
        blockquote { margin: 1em 40px; }

        span, a, strong, b, em, i, u, s, small, sub, sup, code, kbd, samp, var, label,
        abbr, cite, q, mark, time, br, img, input, button, select, textarea, svg,
        big, tt, del, ins { display: inline; }

        strong, b { font-weight: bold; }
        em, i, cite, var { font-style: italic; }
        small { font-size: 0.83em; }
        big { font-size: 1.17em; }
        code, kbd, samp, pre { font-family: Consolas, monospace; }
        pre { white-space: pre; margin: 1em 0; }

        a { color: #0000EE; text-decoration: underline; }
        mark { background-color: #FFFF00; }
        hr { display: block; border: 1px solid #CCCCCC; height: 0; margin: 0.5em 0; }
        """;
}
