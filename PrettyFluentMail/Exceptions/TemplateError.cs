namespace PrettyFluentMail.Exceptions;

/// <summary>
///     A single problem reported while rendering a template.
/// </summary>
/// <param name="Stage">The engine that reported the problem.</param>
/// <param name="Message">A description of the problem.</param>
/// <param name="File">The template file, when known.</param>
/// <param name="Line">The 1-based line number, or 0 when unknown.</param>
/// <param name="Column">The 1-based column number, or 0 when unknown.</param>
public sealed record TemplateError(TemplateStage Stage, string Message, string? File, int Line, int Column)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var location = Line > 0 ? $"({Line},{Column})" : string.Empty;
        return $"[{Stage}] {File}{location}: {Message}";
    }
}
