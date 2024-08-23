using System.Net;
using System.Net.Mail;
using PrettyFluentMail.Configuration;

namespace PrettyFluentMail;

/// <summary>
///     Sends a mail message generated with <see cref="FluentMessage" />
/// </summary>
public class SendMail : SmtpClient
{
    /// <summary>
    ///     Initializes a <see cref="SendMail" /> client
    /// </summary>
    /// <param name="settings">Settings configured with <see cref="SmtpOptions" /></param>
    /// <exception cref="ArgumentNullException">Thrown in the event a mail server is not provided</exception>
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
    ///     Send the mail message
    /// </summary>
    /// <param name="message">Mail message created with <see cref="FluentMessage" /></param>
    public void Send(FluentMessage message)
    {
        Send(message.Build());
    }

    /// <summary>
    ///     Build and send a mail message
    /// </summary>
    public void Send(Action<FluentMessage> message)
    {
        var msg = new FluentMessage();
        message.Invoke(msg);
        Send(msg.Build());
    }
}