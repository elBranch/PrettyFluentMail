using System.Net;
using Scriban;
using Scriban.Parsing;

namespace PrettyFluentMail.Templates;

/// <summary>
///     A Scriban context that HTML-encodes the result of every <c>{{ expression }}</c>, but never the literal
///     markup of the template itself. Values wrapped in <see cref="RawHtml" /> are written as-is.
/// </summary>
internal sealed class HtmlEncodingTemplateContext : TemplateContext
{
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
