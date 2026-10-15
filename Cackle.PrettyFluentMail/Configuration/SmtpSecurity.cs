namespace Cackle.PrettyFluentMail.Configuration;

/// <summary>
///     Describes how the connection to the SMTP server is secured.
/// </summary>
public enum SmtpSecurity
{
    /// <summary>
    ///     Connects in plain text, then upgrades with STARTTLS. The connection fails if the server does not offer STARTTLS.
    ///     This is the default and is normally used with port 587.
    /// </summary>
    StartTls = 0,

    /// <summary>
    ///     Negotiates TLS immediately on connect (implicit TLS). This is normally used with port 465.
    /// </summary>
    SslOnConnect = 1,

    /// <summary>
    ///     Upgrades with STARTTLS when the server offers it, otherwise continues <b>unencrypted</b>. Credentials and message
    ///     content can be sent in clear text. Intended for trusted internal networks only.
    /// </summary>
    StartTlsWhenAvailable = 2,

    /// <summary>
    ///     Never uses TLS. Credentials and message content are sent in clear text. Intended for trusted internal networks
    ///     only.
    /// </summary>
    None = 3
}