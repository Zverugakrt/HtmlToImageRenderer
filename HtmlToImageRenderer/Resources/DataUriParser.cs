using System.Text;

namespace HtmlToImageRenderer.Resources;

/// <summary>
/// Decodes <c>data:</c> URIs, which is how inline Base64 images are embedded in markup.
/// Both the Base64 form and the percent-encoded text form are supported.
/// </summary>
public static class DataUriParser
{
    /// <summary>
    /// Determines whether a source string is a data URI. Leading and trailing white space is
    /// ignored, because markup frequently wraps attribute values.
    /// </summary>
    /// <param name="value">Value of a <c>src</c> attribute.</param>
    /// <returns><c>true</c> when the trimmed value starts with <c>data:</c>.</returns>
    public static bool IsDataUri(string? value) =>
        value is not null && value.TrimStart().StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Decodes the payload of a data URI. White space around the URI and inside the payload is
    /// ignored, so both <c>data:image/jpeg;base64,/9j/4AAQ…</c> and the more readable
    /// <c>data:image/jpeg; base64, /9j/4AAQ…</c>, including line-wrapped payloads, decode to the
    /// same bytes.
    /// </summary>
    /// <param name="value">Complete data URI.</param>
    /// <param name="data">Decoded bytes when the method succeeds.</param>
    /// <param name="mediaType">Media type from the header, for example <c>image/png</c>, when present.</param>
    /// <returns><c>true</c> when a non-empty payload was decoded.</returns>
    public static bool TryDecode(string? value, out byte[] data, out string? mediaType)
    {
        data = Array.Empty<byte>();
        mediaType = null;

        if (!IsDataUri(value))
        {
            return false;
        }

        string uri = value!.Trim();

        int comma = uri.IndexOf(',');
        if (comma < 0)
        {
            return false;
        }

        string header = uri[5..comma];
        string payload = uri[(comma + 1)..];
        bool isBase64 = false;

        foreach (string part in header.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = part.Trim();

            if (string.Equals(trimmed, "base64", StringComparison.OrdinalIgnoreCase))
            {
                isBase64 = true;
                continue;
            }

            if (trimmed.Contains('/'))
            {
                mediaType = trimmed;
            }
        }

        try
        {
            if (isBase64)
            {
                // Percent escapes are decoded first so that %2B, %2F and %3D turn back into real
                // Base64 characters; whatever is left as white space is pure formatting and has to
                // be removed, because Convert.FromBase64String only tolerates it by accident.
                string normalized = StripWhitespace(Uri.UnescapeDataString(payload));
                data = Convert.FromBase64String(normalized);
            }
            else
            {
                data = Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
            }

            return data.Length > 0;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            data = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>
    /// Removes every white-space character from a Base64 payload.
    /// </summary>
    /// <param name="value">Payload text that may contain spaces, tabs or line breaks.</param>
    /// <returns>The payload without white space.</returns>
    private static string StripWhitespace(string value)
    {
        // Payloads are commonly written as "base64, <data>" or wrapped over several lines; none of
        // those characters carry data, so they are removed before decoding.
        var builder = new StringBuilder(value.Length);

        foreach (char character in value)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
