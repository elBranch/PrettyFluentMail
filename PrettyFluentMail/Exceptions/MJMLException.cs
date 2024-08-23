using Mjml.Net;

namespace PrettyFluentMail.Exceptions;

/// <summary>
///     An error occurred while parsing an MJML template
/// </summary>
[Serializable]
public class MJMLException : ApplicationException
{
    /// <summary>
    ///     Initialize a <see cref="MJMLException" />
    /// </summary>
    /// <param name="errors">An MJML provided <see cref="ValidationErrors"/></param>
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
    ///     Error messages received from MJML library
    /// </summary>
    public List<string> Messages { get; }

    /// <summary>
    ///     Error types received from MJML library
    /// </summary>
    public List<string> ErrorTypes { get; }

    /// <summary>
    ///     Files impacted by errors received by MJML library
    /// </summary>
    public List<string?> Files { get; }

    /// <summary>
    ///     Positions of errors received by MJML library
    /// </summary>
    public List<Dictionary<string, int>> Positions { get; }
}