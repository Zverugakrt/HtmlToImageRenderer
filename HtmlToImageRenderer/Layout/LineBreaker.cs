using System.Text;
using HtmlToImageRenderer.Css;
using SkiaSharp;

namespace HtmlToImageRenderer.Layout;

/// <summary>
/// Turns inline text into breakable tokens and measures them. White space handling follows
/// the <c>white-space</c> property: normal and nowrap collapse runs into single spaces,
/// while pre and pre-wrap keep every space and honour explicit newlines.
/// </summary>
public sealed class LineBreaker
{
    /// <summary>
    /// Token representing a single collapsed or preserved space.
    /// </summary>
    public const string SpaceToken = " ";

    /// <summary>
    /// Token representing a hard line break.
    /// </summary>
    public const string NewLineToken = "\n";

    private readonly FontResolver _fonts;

    /// <summary>
    /// Initializes a new instance of the <see cref="LineBreaker"/> class.
    /// </summary>
    /// <param name="fonts">Font cache used for measuring tokens.</param>
    public LineBreaker(FontResolver fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        _fonts = fonts;
    }

    /// <summary>
    /// Splits text into word tokens, space tokens and newline tokens.
    /// </summary>
    /// <param name="text">Raw text of a text node.</param>
    /// <param name="whiteSpace">White-space handling in effect for the text.</param>
    /// <returns>
    /// Tokens in reading order. Words never contain white space; spaces appear as
    /// <see cref="SpaceToken"/> and hard breaks as <see cref="NewLineToken"/>.
    /// </returns>
    public List<string> Tokenize(string? text, WhiteSpaceKind whiteSpace)
    {
        var tokens = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return tokens;
        }

        bool collapse = whiteSpace is WhiteSpaceKind.Normal or WhiteSpaceKind.NoWrap;
        bool keepNewLines = whiteSpace is WhiteSpaceKind.Pre or WhiteSpaceKind.PreWrap;
        var word = new StringBuilder();

        void FlushWord()
        {
            if (word.Length > 0)
            {
                tokens.Add(word.ToString());
                word.Clear();
            }
        }

        void EmitSpace()
        {
            FlushWord();
            if (collapse)
            {
                if (tokens.Count == 0 || tokens[^1] != SpaceToken)
                {
                    tokens.Add(SpaceToken);
                }
            }
            else
            {
                tokens.Add(SpaceToken);
            }
        }

        foreach (char character in text)
        {
            if (character == '\n' && keepNewLines)
            {
                FlushWord();
                tokens.Add(NewLineToken);
                continue;
            }

            // A no-break space belongs to the word: keeping it in the token both preserves its
            // advance width and forbids a line break at that position.
            if (WhitespaceRules.IsCollapsible(character))
            {
                EmitSpace();
                continue;
            }

            word.Append(character);
        }

        FlushWord();
        return tokens;
    }

    /// <summary>
    /// Measures a token with the given font.
    /// </summary>
    /// <param name="token">Token to measure.</param>
    /// <param name="font">Font used for the measurement.</param>
    /// <returns>The advance width in pixels.</returns>
    public float Measure(string token, SKFont font) => _fonts.MeasureText(token, font);

    /// <summary>
    /// Determines whether a token still fits on the current line.
    /// </summary>
    /// <param name="usedWidth">Width already consumed on the line.</param>
    /// <param name="tokenWidth">Width of the token that should be added.</param>
    /// <param name="availableWidth">Width of the line box.</param>
    /// <returns><c>true</c> when the token fits.</returns>
    public static bool Fits(float usedWidth, float tokenWidth, float availableWidth) =>
        usedWidth + tokenWidth <= availableWidth + 0.01f;
}
