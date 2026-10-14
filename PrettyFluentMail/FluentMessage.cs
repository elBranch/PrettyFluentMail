using System.Runtime.CompilerServices;
using MimeKit;
using PrettyFluentMail.Exceptions;
using PrettyFluentMail.Templates;

namespace PrettyFluentMail;

/// <summary>
///     A fluent builder for an email message. Dispose it when done: the built message owns any attachment files and
///     streams, which stay open until then.
/// </summary>
/// <remarks>
///     <see cref="IMailSender.SendAsync(Action{FluentMessage}, CancellationToken)" /> creates and disposes the message for
///     you. Dispose it yourself only when you create one with <c>new FluentMessage()</c>. A message is single-use: once
///     <see cref="Build" /> has been called it can no longer be changed.
/// </remarks>
public sealed class FluentMessage : IDisposable
{
    /// <summary>
    ///     Collection of attachments to be added to the message.
    /// </summary>
    private readonly List<Attachment> _attachments = [];

    /// <summary>
    ///     The underlying MIME message being constructed.
    /// </summary>
    private readonly MimeMessage _message = new();

    /// <summary>
    ///     Indicates whether the message has been built and is now read-only.
    /// </summary>
    private bool _built;

    /// <summary>
    ///     Indicates whether this builder has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    ///     The HTML body content of the message, if provided.
    /// </summary>
    private string? _htmlBody;

    /// <summary>
    ///     The plain text body content of the message, if provided.
    /// </summary>
    private string? _textBody;

    /// <summary>
    ///     Releases the built message and any attachment streams it owns.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _message.Dispose();
    }

    /// <summary>
    ///     Validates the message and returns it. The message stays owned by this builder and is disposed with it. Calling
    ///     <see cref="Build" /> again returns the same instance.
    /// </summary>
    /// <returns>The completed <see cref="MimeMessage" />.</returns>
    /// <exception cref="InvalidOperationException">A sender, a recipient or a body is missing.</exception>
    public MimeMessage Build()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_built) return _message;

        if (_message.From.Count == 0 && _message.Sender is null)
            throw new InvalidOperationException("A sender is required. Call From(...) before building.");
        if (_message.To.Count + _message.Cc.Count + _message.Bcc.Count == 0)
            throw new InvalidOperationException(
                "At least one recipient is required. Call To(...), Cc(...) or Bcc(...).");
        if (_textBody is null && _htmlBody is null)
            throw new InvalidOperationException("A body is required. Call UsingText(...) or UsingTemplate(...).");

        var builder = new BodyBuilder { TextBody = _textBody, HtmlBody = _htmlBody };
        foreach (var attachment in _attachments)
            if (attachment.Path is not null)
                builder.Attachments.Add(attachment.Path);
            else if (attachment.ContentType is null)
                builder.Attachments.Add(attachment.FileName, attachment.Content!);
            else
                builder.Attachments.Add(attachment.FileName, attachment.Content!,
                    ContentType.Parse(attachment.ContentType));

        _message.Body = builder.ToMessageBody();
        _built = true;
        return _message;
    }

    /// <summary>
    ///     Processes a text template by rendering it with Scriban and stores the result as the text body.
    /// </summary>
    /// <param name="template">The Scriban template string.</param>
    /// <param name="model">The model object to pass to the template renderer, or null for no model.</param>
    /// <param name="fileName">The file name for error reporting purposes, or null if not applicable.</param>
    /// <returns>This builder instance for method chaining.</returns>
    private FluentMessage UsingTextTemplateContent(string template, object? model, string? fileName)
    {
        ThrowIfReadOnly();
        _textBody = TemplateRenderer.RenderScriban(template, model, false, fileName);
        return this;
    }

    /// <summary>
    ///     Processes a template by rendering it with Scriban and then as MJML, and stores the result as the HTML body.
    /// </summary>
    /// <param name="template">The Scriban template string.</param>
    /// <param name="model">The model object to pass to the template renderer, or null for no model.</param>
    /// <param name="htmlEncode">Whether to HTML-encode the Scriban output before MJML processing.</param>
    /// <param name="fileName">The file name for error reporting purposes, or null if not applicable.</param>
    /// <returns>This builder instance for method chaining.</returns>
    private FluentMessage UsingTemplateContent(string template, object? model, bool htmlEncode, string? fileName)
    {
        ThrowIfReadOnly();
        var mjml = TemplateRenderer.RenderScriban(template, model, htmlEncode, fileName);
        _htmlBody = TemplateRenderer.RenderMjml(mjml, fileName);
        return this;
    }

    /// <summary>
    ///     Reads the contents of a template file from the specified path.
    /// </summary>
    /// <param name="templatePath">The path to the template file. Must not be null or empty.</param>
    /// <returns>The contents of the template file.</returns>
    private static string ReadTemplate(string templatePath)
    {
        return File.ReadAllText(RequireText(templatePath));
    }

    /// <summary>
    ///     Parses an email address string into a collection of internet addresses.
    /// </summary>
    /// <param name="address">The address string to parse. Supports comma-separated lists. Must not be null or empty.</param>
    /// <returns>An <see cref="InternetAddressList" /> containing the parsed addresses.</returns>
    private static InternetAddressList ParseAddresses(string address)
    {
        return InternetAddressList.Parse(RequireText(address));
    }

    /// <summary>
    ///     Creates a mailbox address with the specified display name and email address.
    /// </summary>
    /// <param name="displayName">The display name for the mailbox. Must not be null.</param>
    /// <param name="address">The email address. Must not be null or empty.</param>
    /// <returns>A <see cref="MailboxAddress" /> with the given display name and parsed address.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="displayName" /> is null.</exception>
    private static MailboxAddress CreateMailbox(string displayName, string address)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        return new MailboxAddress(displayName, MailboxAddress.Parse(RequireText(address)).Address);
    }

    /// <summary>
    ///     Validates that a string value is not null or empty, throwing an appropriate exception if validation fails.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="name">
    ///     The name of the parameter being validated, automatically set via
    ///     <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <returns>The validated string value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is null or empty.</exception>
    private static string RequireText(string value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return value;
    }

    /// <summary>
    ///     Validates that the message has not been built and has not been disposed, throwing an exception if either condition
    ///     is violated.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when this builder has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the message has already been built and is read-only.</exception>
    private void ThrowIfReadOnly()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_built)
            throw new InvalidOperationException("The message has already been built and can no longer be changed.");
    }

    /// <summary>
    ///     Represents an email attachment with optional file path or stream content.
    /// </summary>
    /// <param name="FileName">The name of the attachment file as it should appear in the email.</param>
    /// <param name="Path">The file system path to attach from, or null if attaching from a stream.</param>
    /// <param name="Content">The stream content of the attachment, or null if attaching from a file path.</param>
    /// <param name="ContentType">The MIME content type of the attachment, or null to auto-detect.</param>
    private sealed record Attachment(string FileName, string? Path, Stream? Content, string? ContentType = null);

    #region Addresses

    /// <summary>
    ///     Sets the author of the message ("From").
    /// </summary>
    /// <param name="address">An address such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage From(string address)
    {
        ThrowIfReadOnly();
        _message.From.Clear();
        _message.From.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Sets the author of the message ("From").
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="address">The email address.</param>
    public FluentMessage From(string displayName, string address)
    {
        ThrowIfReadOnly();
        _message.From.Clear();
        _message.From.Add(CreateMailbox(displayName, address));
        return this;
    }

    /// <summary>
    ///     Sets the "Sender" header, for a message sent by someone other than its author. The SMTP envelope sender is taken
    ///     from "From" (or "Sender" when present).
    /// </summary>
    /// <param name="address">An address such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage Sender(string address)
    {
        ThrowIfReadOnly();
        _message.Sender = MailboxAddress.Parse(RequireText(address));
        return this;
    }

    /// <summary>
    ///     Adds one or more recipients ("To"). Accepts a comma-separated list.
    /// </summary>
    /// <param name="address">An address such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage To(string address)
    {
        ThrowIfReadOnly();
        _message.To.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Adds a recipient ("To").
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="address">The email address.</param>
    public FluentMessage To(string displayName, string address)
    {
        ThrowIfReadOnly();
        _message.To.Add(CreateMailbox(displayName, address));
        return this;
    }

    /// <summary>
    ///     Adds several recipients ("To").
    /// </summary>
    /// <param name="addresses">Addresses such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage To(IEnumerable<string> addresses)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(addresses);
        foreach (var address in addresses) _message.To.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Adds one or more carbon-copy recipients ("Cc"). Accepts a comma-separated list.
    /// </summary>
    /// <param name="address">An address such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage Cc(string address)
    {
        ThrowIfReadOnly();
        _message.Cc.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Adds a carbon-copy recipient ("Cc").
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="address">The email address.</param>
    public FluentMessage Cc(string displayName, string address)
    {
        ThrowIfReadOnly();
        _message.Cc.Add(CreateMailbox(displayName, address));
        return this;
    }

    /// <summary>
    ///     Adds several carbon-copy recipients ("Cc").
    /// </summary>
    /// <param name="addresses">Addresses such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage Cc(IEnumerable<string> addresses)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(addresses);
        foreach (var address in addresses) _message.Cc.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Adds one or more blind-carbon-copy recipients ("Bcc"). Accepts a comma-separated list.
    /// </summary>
    /// <param name="address">An address such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage Bcc(string address)
    {
        ThrowIfReadOnly();
        _message.Bcc.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Adds a blind-carbon-copy recipient ("Bcc").
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="address">The email address.</param>
    public FluentMessage Bcc(string displayName, string address)
    {
        ThrowIfReadOnly();
        _message.Bcc.Add(CreateMailbox(displayName, address));
        return this;
    }

    /// <summary>
    ///     Adds several blind-carbon-copy recipients ("Bcc").
    /// </summary>
    /// <param name="addresses">Addresses such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage Bcc(IEnumerable<string> addresses)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(addresses);
        foreach (var address in addresses) _message.Bcc.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Adds an address that replies should be sent to ("Reply-To").
    /// </summary>
    /// <param name="address">An address such as <c>Name &lt;name@example.com&gt;</c>.</param>
    public FluentMessage ReplyTo(string address)
    {
        ThrowIfReadOnly();
        _message.ReplyTo.AddRange(ParseAddresses(address));
        return this;
    }

    /// <summary>
    ///     Adds an address that replies should be sent to ("Reply-To").
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="address">The email address.</param>
    public FluentMessage ReplyTo(string displayName, string address)
    {
        ThrowIfReadOnly();
        _message.ReplyTo.Add(CreateMailbox(displayName, address));
        return this;
    }

    #endregion

    #region Headers

    /// <summary>
    ///     Sets the subject of the message.
    /// </summary>
    /// <param name="subject">The subject text.</param>
    public FluentMessage WithSubject(string subject)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(subject);
        _message.Subject = subject;
        return this;
    }

    /// <summary>
    ///     Sets the priority of the message.
    /// </summary>
    /// <param name="priority">The priority.</param>
    public FluentMessage WithPriority(MessagePriority priority)
    {
        ThrowIfReadOnly();
        _message.Priority = priority;
        return this;
    }

    /// <summary>
    ///     Adds a custom header, such as <c>X-Campaign-Id</c>.
    /// </summary>
    /// <param name="name">The header field name.</param>
    /// <param name="value">The header value.</param>
    public FluentMessage WithHeader(string name, string value)
    {
        ThrowIfReadOnly();
        _message.Headers.Add(RequireText(name), RequireText(value));
        return this;
    }

    #endregion

    #region Body

    /// <summary>
    ///     Sets the plain text body. When an HTML body is also set, the message is sent as <c>multipart/alternative</c> so
    ///     clients can pick either one.
    /// </summary>
    /// <param name="body">The plain text content.</param>
    public FluentMessage UsingText(string body)
    {
        ThrowIfReadOnly();
        _textBody = RequireText(body);
        return this;
    }

    /// <summary>
    ///     Sets the plain text body from a Scriban template file. Values are not HTML-encoded.
    /// </summary>
    /// <param name="templatePath">Path to the Scriban text template.</param>
    /// <param name="model">An object whose public properties become template variables.</param>
    /// <exception cref="TemplateRenderException">The template has errors.</exception>
    public FluentMessage UsingTextTemplate(string templatePath, object? model = null)
    {
        return UsingTextTemplateContent(ReadTemplate(templatePath), model, templatePath);
    }

    /// <summary>
    ///     Sets the plain text body from Scriban template text. Values are not HTML-encoded.
    /// </summary>
    /// <param name="template">The Scriban template text.</param>
    /// <param name="model">An object whose public properties become template variables.</param>
    /// <exception cref="TemplateRenderException">The template has errors.</exception>
    public FluentMessage UsingTextTemplateContent(string template, object? model = null)
    {
        return UsingTextTemplateContent(RequireText(template), model, null);
    }

    /// <summary>
    ///     Sets the HTML body from an MJML template file with Scriban variables.
    /// </summary>
    /// <param name="templatePath">Path to the MJML template.</param>
    /// <param name="model">An object whose public properties become template variables (snake_case names).</param>
    /// <param name="htmlEncode">
    ///     Whether every <c>{{ expression }}</c> is HTML-encoded. The default is <see langword="true" />,
    ///     which prevents markup in variable values from reaching the email. Pass <see langword="false" /> only when every
    ///     value is trusted, or mark individual values with <c>{{ value | raw }}</c> instead.
    /// </param>
    /// <exception cref="TemplateRenderException">The Scriban or MJML template has errors.</exception>
    public FluentMessage UsingTemplate(string templatePath, object? model = null, bool htmlEncode = true)
    {
        return UsingTemplateContent(ReadTemplate(templatePath), model, htmlEncode, templatePath);
    }

    /// <summary>
    ///     Sets the HTML body from MJML template text with Scriban variables. Use this to load templates from embedded
    ///     resources, a database or any other source.
    /// </summary>
    /// <param name="template">The MJML template text.</param>
    /// <param name="model">An object whose public properties become template variables (snake_case names).</param>
    /// <param name="htmlEncode">See <see cref="UsingTemplate" />.</param>
    /// <exception cref="TemplateRenderException">The Scriban or MJML template has errors.</exception>
    public FluentMessage UsingTemplateContent(string template, object? model = null, bool htmlEncode = true)
    {
        return UsingTemplateContent(RequireText(template), model, htmlEncode, null);
    }

    #endregion

    #region Attachments

    /// <summary>
    ///     Attaches a file. The file is opened when the message is built and released when it is disposed. Never pass a path
    ///     that comes from user input without validating it first.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public FluentMessage AddAttachment(string filePath)
    {
        ThrowIfReadOnly();
        var fullPath = Path.GetFullPath(RequireText(filePath));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The attachment file was not found.", fullPath);

        _attachments.Add(new Attachment(Path.GetFileName(fullPath), fullPath, null));
        return this;
    }

    /// <summary>
    ///     Attaches the content of a stream. The message takes ownership and disposes the stream.
    /// </summary>
    /// <param name="fileName">The file name shown to the recipient.</param>
    /// <param name="content">The content, read from its current position.</param>
    /// <param name="contentType">The MIME type; inferred from <paramref name="fileName" /> when omitted.</param>
    public FluentMessage AddAttachment(string fileName, Stream content, string? contentType = null)
    {
        ThrowIfReadOnly();
        ArgumentNullException.ThrowIfNull(content);
        _attachments.Add(new Attachment(Path.GetFileName(RequireText(fileName)), null, content, contentType));
        return this;
    }

    #endregion
}