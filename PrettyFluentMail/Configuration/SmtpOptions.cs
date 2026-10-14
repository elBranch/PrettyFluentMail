namespace PrettyFluentMail.Configuration;

/// <summary>
///     The connection parameters required to send mail through an SMTP server.
///     The defaults are secure: STARTTLS is required and certificates are fully validated.
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>
    ///     Gets or sets the host name or IP address of the mail server. This property is required.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    ///     Gets or sets the port number of the mail server. The default is 587 (submission).
    ///     Use 465 with <see cref="SmtpSecurity.SslOnConnect" />.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    ///     Gets or sets how the connection is secured. The default is <see cref="SmtpSecurity.StartTls" />.
    ///     Set <see cref="SmtpSecurity.StartTlsWhenAvailable" /> or <see cref="SmtpSecurity.None" /> to opt in to
    ///     an unencrypted connection.
    /// </summary>
    public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;

    /// <summary>
    ///     Gets or sets the user name used for authentication. Leave <see langword="null" /> when the
    ///     server does not require authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Gets or sets the password used for authentication. Never commit this value to source control;
    ///     prefer user secrets, environment variables or a secret store.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    ///     Gets or sets the timeout applied to each network operation. The default is 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets or sets a value indicating whether certificate revocation lists are checked. The default is
    ///     <see langword="true" />.
    /// </summary>
    public bool CheckCertificateRevocation { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether any server certificate is accepted, including self-signed,
    ///     expired or mismatched ones. The default is <see langword="false" />. Enabling this removes protection
    ///     against man-in-the-middle attacks; use it only on trusted internal networks.
    /// </summary>
    public bool AllowUntrustedCertificates { get; set; }

    /// <summary>
    ///     Checks the options and returns a description of every problem found.
    /// </summary>
    /// <returns>An empty sequence when the options are valid.</returns>
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            yield return $"{nameof(Host)} is required.";

        if (Port is < 1 or > 65535)
            yield return $"{nameof(Port)} must be between 1 and 65535.";

        if (!Enum.IsDefined(Security))
            yield return $"{nameof(Security)} is not a valid {nameof(SmtpSecurity)} value.";

        if (Timeout <= TimeSpan.Zero)
            yield return $"{nameof(Timeout)} must be greater than zero.";

        if (!string.IsNullOrEmpty(Password) && string.IsNullOrEmpty(Username))
            yield return $"{nameof(Username)} is required when {nameof(Password)} is set.";
    }
}
