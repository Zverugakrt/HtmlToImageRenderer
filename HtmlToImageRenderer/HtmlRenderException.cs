namespace HtmlToImageRenderer;

/// <summary>
/// Raised when markup cannot be rendered, for example because the input is missing
/// or an internal rendering stage fails unexpectedly.
/// </summary>
public sealed class HtmlRenderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlRenderException"/> class.
    /// </summary>
    /// <param name="message">Human readable description of the failure.</param>
    public HtmlRenderException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlRenderException"/> class
    /// with an inner exception that caused the failure.
    /// </summary>
    /// <param name="message">Human readable description of the failure.</param>
    /// <param name="innerException">Underlying exception, if any.</param>
    public HtmlRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Throws an <see cref="HtmlRenderException"/> when <paramref name="html"/> is
    /// <c>null</c>, which is the only input the renderer rejects outright.
    /// </summary>
    /// <param name="html">Markup supplied by the caller.</param>
    /// <param name="paramName">Name of the calling parameter, used in the message.</param>
    /// <exception cref="HtmlRenderException">Thrown when <paramref name="html"/> is <c>null</c>.</exception>
    public static void ThrowIfNullHtml(string? html, string paramName = "html")
    {
        if (html is null)
        {
            throw new HtmlRenderException($"{paramName} must not be null. Pass an empty string to render a blank page.");
        }
    }
}
