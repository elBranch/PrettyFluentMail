using System.Net;
using System.Net.Mail;
using PrettyFluentMail.Configuration;

namespace PrettyFluentMail;

/// <summary>
///     Sends a mail message generated with <see cref="FluentMessage" />.
///     Inherits from <see cref="SmtpClient" /> and provides convenient methods for sending emails
///     using configuration specified in <see cref="SmtpOptions" />.
/// </summary>
public class SendMail : SmtpClient
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SendMail" /> class using the specified SMTP settings.
    /// </summary>
    /// <param name="settings">Settings configured with <see cref="SmtpOptions" />.</param>
    /// <exception cref="ArgumentNullException">Thrown if a mail server is not provided in <paramref name="settings"/>.</exception>
    public SendMail(SmtpOptions settings)
    {
        Host = settings.Host ?? throw new ArgumentNullException(nameof(settings), "No mail server provided");
        var username = settings.Username ?? string.Empty;
        var password = settings.Password ?? string.Empty;
        Credentials = new NetworkCredential(username, password);

        Port = settings.Port;
        EnableSsl = settings.EnableSsl;
    }

    /// <summary>
    ///     Sends the specified mail message.
    /// </summary>
    /// <param name="message">Mail message created with <see cref="FluentMessage" />.</param>
    public void Send(FluentMessage message)
    {
        Send(message.Build());
    }

    /// <summary>
    ///     Builds and sends a mail message using a fluent configuration action.
    /// </summary>
    /// <param name="message">An action that configures a <see cref="FluentMessage" /> instance.</param>
    public void Send(Action<FluentMessage> message)
    {
        var msg = new FluentMessage();
        message.Invoke(msg);
        Send(msg.Build());
    }
}