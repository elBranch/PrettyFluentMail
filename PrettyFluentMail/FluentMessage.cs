using System.Net.Mail;
using System.Text;
using Mjml.Net;
using PrettyFluentMail.Exceptions;
using Scriban;

namespace PrettyFluentMail;

/// <summary>
///     Attributes of a mail message and fluent methods for building an email.
/// </summary>
public class FluentMessage
{
    // The underlying MailMessage instance used to construct the email.
    private readonly MailMessage _message = new() {BodyEncoding = Encoding.UTF8};

    /// <summary>
    ///     Sets the sender's address of the email.
    /// </summary>
    /// <param name="address">
    ///     Email address in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage From(string address)
    {
        _message.From = new MailAddress(address);
        return this;
    }

    /// <summary>
    ///     Sets the sender (envelope sender) of the email.
    /// </summary>
    /// <param name="address">
    ///     Email address in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage Sender(string address)
    {
        _message.Sender = new MailAddress(address);
        return this;
    }

    /// <summary>
    ///     Adds a recipient to the "To" field.
    /// </summary>
    /// <param name="address">
    ///     Email address in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage To(string address)
    {
        _message.To.Add(address);
        return this;
    }

    /// <summary>
    ///     Adds multiple recipients to the "To" field.
    /// </summary>
    /// <param name="addresses">
    ///     Enumerable of email addresses in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage To(IEnumerable<string> addresses)
    {
        foreach (var address in addresses) _message.To.Add(address);
        return this;
    }

    /// <summary>
    ///     Adds a recipient to the "CC" field.
    /// </summary>
    /// <param name="address">
    ///     Email address in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage Cc(string address)
    {
        _message.CC.Add(address);
        return this;
    }

    /// <summary>
    ///     Adds multiple recipients to the "CC" field.
    /// </summary>
    /// <param name="addresses">
    ///     Enumerable of email addresses in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage Cc(IEnumerable<string> addresses)
    {
        foreach (var address in addresses) _message.CC.Add(address);
        return this;
    }

    /// <summary>
    ///     Adds a recipient to the "BCC" field.
    /// </summary>
    /// <param name="address">
    ///     Email address in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage Bcc(string address)
    {
        _message.Bcc.Add(address);
        return this;
    }

    /// <summary>
    ///     Adds multiple recipients to the "BCC" field.
    /// </summary>
    /// <param name="addresses">
    ///     Enumerable of email addresses in the format "Name &lt;name@contosso.com&gt;".
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage Bcc(IEnumerable<string> addresses)
    {
        foreach (var address in addresses) _message.Bcc.Add(address);
        return this;
    }

    /// <summary>
    ///     Sets the subject of the email message.
    /// </summary>
    /// <param name="subject">Subject of the email message.</param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage WithSubject(string subject)
    {
        _message.Subject = subject;
        return this;
    }

    /// <summary>
    ///     Sets a plain text body for the email message.
    /// </summary>
    /// <param name="body">
    ///     The plain text content of the email body.
    /// </param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage UsingText(string body)
    {
        _message.IsBodyHtml = false;
        _message.Body = body;
        return this;
    }

    /// <summary>
    ///     Uses an MJML template with the Scriban template engine to generate an HTML email body.
    /// </summary>
    /// <param name="templatePath">Full path to the MJML template file.</param>
    /// <param name="variables">An object containing properties for template variable substitution.</param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    /// <exception cref="ScribanException">Thrown if there are errors in the Scriban template.</exception>
    /// <exception cref="MJMLException">Thrown if there are errors in the MJML rendering.</exception>
    public FluentMessage UsingTemplate(string templatePath, object? variables = null)
    {
        string templateContent;
        using (var file = File.OpenText(templatePath))
        {
            templateContent = file.ReadToEnd();
        }

        _message.IsBodyHtml = true;
        _message.Body = templatePath;

        // Let's run the template through Scriban to replace template variables first
        var parsed = Template.Parse(templateContent);
        if (parsed.HasErrors)
            foreach (var error in parsed.Messages)
                throw new ScribanException(error);

        var content = parsed.Render(variables);

        // Now let's make the template pretty with HTML
        var mjml = new MjmlRenderer();
        var mjmlOptions = new MjmlOptions();
        var render = mjml.Render(content, mjmlOptions);
        if (render.Errors.Any()) throw new MJMLException(render.Errors);

        _message.Body = render.Html;

        return this;
    }

    /// <summary>
    ///     Adds a file attachment to the email message.
    /// </summary>
    /// <param name="filePath">Path to the file to attach.</param>
    /// <returns>The current <see cref="FluentMessage"/> instance.</returns>
    public FluentMessage AddAttachment(string filePath)
    {
        _message.Attachments.Add(new Attachment(filePath));
        return this;
    }

    /// <summary>
    ///     Returns the constructed <see cref="MailMessage"/> instance.
    /// </summary>
    /// <returns>The built <see cref="MailMessage"/> object.</returns>
    public MailMessage Build()
    {
        return _message;
    }
}