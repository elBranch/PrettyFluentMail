using System.Net.Mail;
using System.Text;
using Mjml.Net;
using PrettyFluentMail.Exceptions;
using Scriban;

namespace PrettyFluentMail;

/// <summary>
///     Attributes of a mail message
/// </summary>
public class FluentMessage
{
    private readonly MailMessage _message = new() {BodyEncoding = Encoding.UTF8};

    /// <summary>
    ///     <inheritdoc cref="MailMessage.From" />
    /// </summary>
    /// <param name="address">
    ///     Email address
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage From(string address)
    {
        _message.From = new MailAddress(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.Sender" />
    /// </summary>
    /// <param name="address">
    ///     Email address
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage Sender(string address)
    {
        _message.Sender = new MailAddress(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.To" />
    /// </summary>
    /// <param name="address">
    ///     Email address
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage To(string address)
    {
        _message.To.Add(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.To" />
    /// </summary>
    /// <param name="addresses">
    ///     Enumerable of addresses
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage To(IEnumerable<string> addresses)
    {
        foreach (var address in addresses) _message.To.Add(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.CC" />
    /// </summary>
    /// <param name="address">
    ///     Email address
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage Cc(string address)
    {
        _message.CC.Add(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.CC" />
    /// </summary>
    /// <param name="addresses">
    ///     Enumerable of addresses
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage Cc(IEnumerable<string> addresses)
    {
        foreach (var address in addresses) _message.CC.Add(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.CC" />
    /// </summary>
    /// <param name="address">
    ///     Email address
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage Bcc(string address)
    {
        _message.Bcc.Add(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.Bcc" />
    /// </summary>
    /// <param name="addresses">
    ///     Enumerable of addresses
    ///     <example>Name &lt;name@contosso.com&gt;</example>
    /// </param>
    public FluentMessage Bcc(IEnumerable<string> addresses)
    {
        foreach (var address in addresses) _message.Bcc.Add(address);
        return this;
    }

    /// <summary>
    ///     <inheritdoc cref="MailMessage.Subject" />
    /// </summary>
    /// <param name="subject">Subject of email message</param>
    public FluentMessage WithSubject(string subject)
    {
        _message.Subject = subject;
        return this;
    }

    /// <summary>
    ///     Sets a text based body on the email message
    /// </summary>
    /// <param name="body">
    ///     <inheritdoc cref="MailMessage.Body" />
    /// </param>
    public FluentMessage UsingText(string body)
    {
        _message.IsBodyHtml = false;
        _message.Body = body;
        return this;
    }

    /// <summary>
    ///     Uses an MJML template with the Scriban template engine to generate an HTML message
    /// </summary>
    /// <param name="templatePath">Full path to MJML file</param>
    /// <param name="variables">An object containing properties whose values will be used by Scriban</param>
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
    ///     <inheritdoc cref="MailMessage.Attachments" />
    /// </summary>
    /// <param name="filePath">Path to file</param>
    public FluentMessage AddAttachment(string filePath)
    {
        _message.Attachments.Add(new Attachment(filePath));
        return this;
    }

    /// <summary>
    ///     Returns the built MailMessage
    /// </summary>
    /// <returns></returns>
    public MailMessage Build()
    {
        return _message;
    }
}