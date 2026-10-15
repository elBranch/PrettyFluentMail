using System.Net;
using Scriban;
using Scriban.Parsing;

namespace Cackle.PrettyFluentMail.Templates;

/// <summary>
///     A Scriban context that HTML-encodes the result of every <c>{{ expression }}</c>, but never the literal markup of
///     the template itself. Values wrapped in <see cref="RawHtml" /> are written as-is.
/// </summary>
internal sealed class HtmlEncodingTemplateContext : TemplateContext
{
    /// <summary>
    ///     Writes text to the template output, applying HTML encoding to expression results while preserving raw markup.
    /// </summary>
    /// <param name="span">The source span location in the template where this output originated.</param>
    /// <param name="textAsObject">
    ///     The value to write. If null, nothing is written. If of type <see cref="RawHtml" />, the
    ///     value is written without encoding. Otherwise, the value is HTML-encoded.
    /// </param>
    /// <returns>This context instance for method chaining.</returns>
    public override TemplateContext Write(SourceSpan span, object? textAsObject)
    {
        return textAsObject switch
        {
            null => this,
            RawHtml raw => base.Write(span, raw.Value),
            _ => base.Write(span, WebUtility.HtmlEncode(ObjectToString(textAsObject)))
        };
    }
}