namespace Cackle.PrettyFluentMail.Exceptions;

/// <summary>
///     The exception that is thrown when a Scriban or MJML template cannot be rendered.
///     <see cref="Errors" /> lists every problem; <see cref="Exception.InnerException" /> carries the
///     underlying engine exception when there is one.
/// </summary>
public sealed class TemplateRenderException : Exception
{
    private static readonly IReadOnlyList<TemplateError> NoErrors = [];

    /// <summary>Initializes a new instance of the <see cref="TemplateRenderException" /> class.</summary>
    public TemplateRenderException() : this("The template could not be rendered.")
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public TemplateRenderException(string message) : base(message)
    {
        Errors = NoErrors;
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public TemplateRenderException(string message, Exception? innerException) : base(message, innerException)
    {
        Errors = NoErrors;
    }

    /// <summary>Initializes a new instance from the problems reported by a template engine.</summary>
    /// <param name="errors">The reported problems. At least one is required.</param>
    /// <param name="innerException">The underlying engine exception, if any.</param>
    public TemplateRenderException(IReadOnlyList<TemplateError> errors, Exception? innerException = null)
        : base(BuildMessage(errors), innerException)
    {
        Errors = errors;
    }

    /// <summary>Gets every problem reported while rendering.</summary>
    public IReadOnlyList<TemplateError> Errors { get; }

    private static string BuildMessage(IReadOnlyList<TemplateError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
            throw new ArgumentException("At least one error is required.", nameof(errors));

        return errors.Count == 1
            ? errors[0].ToString()
            : $"{errors.Count} template errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
    }
}
