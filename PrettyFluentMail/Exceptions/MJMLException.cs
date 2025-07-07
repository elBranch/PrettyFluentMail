using Mjml.Net;

namespace PrettyFluentMail.Exceptions;

/// <summary>
///     Represents an exception that is thrown when an error occurs while parsing an MJML template.
/// </summary>
[Serializable]
public class MJMLException : ApplicationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MJMLException"/> class with the specified MJML validation errors.
    /// </summary>
    /// <param name="errors">A collection of MJML <see cref="ValidationErrors"/> that describe the parsing errors.</param>
    public MJMLException(ValidationErrors errors) : base(errors[0].Error)
    {
        Messages = new List<string>();
        ErrorTypes = new List<string>();
        Files = new List<string?>();
        Positions = new List<Dictionary<string, int>>();

        foreach (var error in errors)
        {
            Messages.Add(error.Error);
            ErrorTypes.Add(error.Type.ToString("G"));
            Files.Add(error.Position.File);
            Positions.Add(new Dictionary<string, int>
            {
                {nameof(error.Position.LineNumber), error.Position.LineNumber},
                {nameof(error.Position.LinePosition), error.Position.LinePosition}
            });
        }
    }

    /// <summary>
    ///     Gets the error messages received from the MJML library.
    /// </summary>
    public List<string> Messages { get; }

    /// <summary>
    ///     Gets the error types received from the MJML library.
    /// </summary>
    public List<string> ErrorTypes { get; }

    /// <summary>
    ///     Gets the files impacted by errors received from the MJML library.
    /// </summary>
    public List<string?> Files { get; }

    /// <summary>
    ///     Gets the positions of errors received from the MJML library.
    ///     Each dictionary contains line number and line position information.
    /// </summary>
    public List<Dictionary<string, int>> Positions { get; }
}