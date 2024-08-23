namespace PrettyFluentMail.Configuration;

/// <summary>
///     Connection parameters for SMTP server.
/// </summary>
public class SmtpOptions
{
    /// <summary>
    ///     Host name of mail server.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    ///     Port number of mail server; defaults to 25.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    ///     Enable SSL for the mail server; defaults to false.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    ///     Username for mail server authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Password for mail server authentication.
    /// </summary>
    public string? Password { get; set; }
}