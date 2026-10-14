namespace PrettyFluentMail.Templates;

/// <summary>
///     Marks a string as trusted HTML so it is written to the template output without HTML encoding. Only wrap content you
///     control; never wrap user-supplied input.
/// </summary>
/// <param name="Value">The HTML to emit as-is.</param>
public readonly record struct RawHtml(string Value)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}