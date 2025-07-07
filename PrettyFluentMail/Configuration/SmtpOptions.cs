namespace PrettyFluentMail.Configuration;

/// <summary>
///     Represents the connection parameters required to configure an SMTP server.
///     This class is typically used to provide settings for sending emails via SMTP.
/// </summary>
public class SmtpOptions
{
    /// <summary>
    ///     Gets or sets the host name or IP address of the mail server.
    ///     This property is required.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    ///     Gets or sets the port number of the mail server.
    ///     The default value is 25, which is the standard SMTP port.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    ///     Gets or sets a value indicating whether SSL should be enabled for the mail server connection.
    ///     The default value is false.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    ///     Gets or sets the username used for mail server authentication.
    ///     This property is optional and may be null if authentication is not required.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Gets or sets the password used for mail server authentication.
    ///     This property is optional and may be null if authentication is not required.
    /// </summary>
    public string? Password { get; set; }
}