namespace PrettyFluentMail.Exceptions;

/// <summary>
///     Identifies the template engine that reported a <see cref="TemplateError" />.
/// </summary>
public enum TemplateStage
{
    /// <summary>The Scriban variable substitution stage.</summary>
    Scriban,

    /// <summary>The MJML to HTML rendering stage.</summary>
    Mjml
}
