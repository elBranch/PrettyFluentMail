using Scriban.Parsing;

namespace PrettyFluentMail.Exceptions;

/// <summary>
///     Error occurred while processing a Scriban template
/// </summary>
[Serializable]
public class ScribanException : ApplicationException
{
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

    public bool IsEmpty { get; }
    public string FileName { get; }
    public Dictionary<string, int> Start { get; }
    public Dictionary<string, int> End { get; }
    public int Length { get; }
}