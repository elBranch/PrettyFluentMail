using Scriban.Parsing;

namespace PrettyFluentMail.Exceptions;

/// <summary>
///     Represents an exception that is thrown when an error occurs while processing a Scriban template.
/// </summary>
[Serializable]
public class ScribanException : ApplicationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ScribanException"/> class with the specified Scriban log message.
    /// </summary>
    /// <param name="message">A Scriban provided <see cref="LogMessage"/> containing error details.</param>
    public ScribanException(LogMessage message) :
        base(message.Message)
    {
        IsEmpty = message.Span.IsEmpty;
        FileName = message.Span.FileName;

        Start = new Dictionary<string, int>
        {
            {nameof(message.Span.Start.Column), message.Span.Start.Column},
            {nameof(message.Span.Start.Line), message.Span.Start.Line},
            {nameof(message.Span.Start.Offset), message.Span.Start.Offset}
        };

        End = new Dictionary<string, int>
        {
            {nameof(message.Span.End.Column), message.Span.End.Column},
            {nameof(message.Span.End.Line), message.Span.End.Line},
            {nameof(message.Span.End.Offset), message.Span.End.Offset}
        };

        Length = message.Span.Length;
    }

    /// <summary>
    ///     Gets a value indicating whether the template is empty.
    /// </summary>
    public bool IsEmpty { get; }

    /// <summary>
    ///     Gets the file name of the impacted file.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    ///     Gets the start positions of the error, including column, line, and offset.
    /// </summary>
    public Dictionary<string, int> Start { get; }

    /// <summary>
    ///     Gets the end positions of the error, including column, line, and offset.
    /// </summary>
    public Dictionary<string, int> End { get; }

    /// <summary>
    ///     Gets the length of the error span in the template.
    /// </summary>
    public int Length { get; }
}