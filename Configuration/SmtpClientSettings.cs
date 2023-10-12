namespace PrettyFluentMail.Configuration;

/// <summary>
///     SMTP settings for the library
/// </summary>
public class SmtpClientSettings
{
    /// <summary>
    ///     Host name of your mail server
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    ///     Port number of your mail server, defaults to 25
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    ///     Enable SSL for the mail server, defaults to false
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    ///     Username to login to mail server
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Password to login to mail server
    /// </summary>
    public string? Password { get; set; }
}