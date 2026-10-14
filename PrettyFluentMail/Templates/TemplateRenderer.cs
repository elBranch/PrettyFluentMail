using System.Reflection;
using Mjml.Net;
using PrettyFluentMail.Exceptions;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace PrettyFluentMail.Templates;

/// <summary>
///     Renders templates in two stages: Scriban substitutes variables, then MJML turns the result into HTML.
/// </summary>
internal static class TemplateRenderer
{
    /// <summary>
    ///     Maximum number of loop iterations allowed in Scriban templates.
    /// </summary>
    private const int LoopLimit = 10_000;

    /// <summary>
    ///     Maximum recursion depth allowed in Scriban template functions.
    /// </summary>
    private const int RecursionLimit = 100;

    /// <summary>
    ///     Maximum size of rendered template output in characters.
    /// </summary>
    private const int OutputLimit = 10_000_000;

    /// <summary>
    ///     Timeout duration for regular expression operations in templates.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Shared MJML renderer instance. Designed to be created once and reused for performance.
    /// </summary>
    private static readonly MjmlRenderer Mjml = new();

    /// <summary>
    ///     Substitutes variables into <paramref name="template" /> using Scriban.
    /// </summary>
    /// <param name="template">The Scriban template text.</param>
    /// <param name="model">The object whose public properties become template variables (snake_case names).</param>
    /// <param name="htmlEncode">Whether to HTML-encode every <c>{{ expression }}</c> result.</param>
    /// <param name="fileName">The template file name, used in error messages.</param>
    public static string RenderScriban(string template, object? model, bool htmlEncode, string? fileName = null)
    {
        var parsed = Template.Parse(template, fileName);
        var parseErrors = parsed.Messages
            .Where(m => m.Type == ParserMessageType.Error)
            .Select(m => ToError(m, fileName))
            .ToList();
        if (parseErrors.Count > 0)
            throw new TemplateRenderException(parseErrors);

        var context = CreateContext(htmlEncode);
        if (model is not null)
            context.PushGlobal(ScriptObject.From(model));

        try
        {
            return parsed.Render(context);
        }
        catch (ScriptRuntimeException ex)
        {
            throw new TemplateRenderException([ToError(ex.OriginalMessage, ex.Span, fileName)], ex);
        }
    }

    /// <summary>
    ///     Converts MJML markup to HTML.
    /// </summary>
    public static string RenderMjml(string mjml, string? fileName = null)
    {
        var result = Mjml.Render(mjml, new MjmlOptions());
        if (result.Errors.Count <= 0) return result.Html;

        var errors = result.Errors
            .Select(e => new TemplateError(
                TemplateStage.Mjml,
                e.Error,
                e.Position.File ?? fileName,
                e.Position.LineNumber,
                e.Position.LinePosition))
            .ToList();

        throw new TemplateRenderException(errors);
    }

    /// <summary>
    ///     Creates a Scriban template context with configured limits, security settings, and a <c>raw</c> function.
    /// </summary>
    /// <param name="htmlEncode">
    ///     If true, creates an <see cref="HtmlEncodingTemplateContext" /> that HTML-encodes expression
    ///     results; otherwise creates a standard <see cref="TemplateContext" />.
    /// </param>
    /// <returns>A configured template context ready for rendering.</returns>
    private static TemplateContext CreateContext(bool htmlEncode)
    {
        var context = htmlEncode ? new HtmlEncodingTemplateContext() : new TemplateContext();

        // Fail loudly on typos such as {{ nmae }} instead of emailing a blank.
        context.StrictVariables = true;
        context.LoopLimit = LoopLimit;
        context.LoopLimitQueryable = LoopLimit;
        context.RecursiveLimit = RecursionLimit;
        context.OutputLimit = OutputLimit;
        context.RegexTimeOut = RegexTimeout;

        // Templates read data; they cannot call methods on the model. No TemplateLoader is set, so
        // {{ include }} cannot read files either.
        context.MemberFilter = static member => member is PropertyInfo or FieldInfo;

        context.BuiltinObject.Import("raw",
            new Func<object?, RawHtml>(static value => new RawHtml(value?.ToString() ?? string.Empty)));
        return context;
    }

    /// <summary>
    ///     Converts a Scriban <see cref="LogMessage" /> to a <see cref="TemplateError" />.
    /// </summary>
    /// <param name="message">The log message from Scriban parsing.</param>
    /// <param name="fileName">The template file name to use if the message doesn't contain one, or null if not applicable.</param>
    /// <returns>A <see cref="TemplateError" /> representing the log message.</returns>
    private static TemplateError ToError(LogMessage message, string? fileName)
    {
        return ToError(message.Message, message.Span, fileName);
    }

    /// <summary>
    ///     Converts an error message and source span to a <see cref="TemplateError" />.
    /// </summary>
    /// <param name="message">The error message text.</param>
    /// <param name="span">The source span indicating where the error occurred in the template.</param>
    /// <param name="fileName">The template file name to use if the span doesn't contain one, or null if not applicable.</param>
    /// <returns>A <see cref="TemplateError" /> with normalized line and column numbers (1-based instead of 0-based).</returns>
    private static TemplateError ToError(string message, SourceSpan span, string? fileName)
    {
        return new TemplateError(
            TemplateStage.Scriban,
            message,
            string.IsNullOrEmpty(span.FileName) ? fileName : span.FileName,
            span.Start.Line + 1,
            span.Start.Column + 1);
    }
}