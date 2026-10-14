using MailKit.Net.Smtp;
using MailKit.Security;
using PrettyFluentMail.Configuration;

namespace PrettyFluentMail;

/// <summary>
///     Sends mail through an SMTP server using MailKit. A new connection is opened and closed for each message,
///     so one instance can be shared (registered as a singleton) and used from multiple threads.
/// </summary>
public sealed class SmtpMailSender : IMailSender
{
    private readonly SmtpOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SmtpMailSender" /> class.
    /// </summary>
    /// <param name="options">The server settings. They are validated and copied.</param>
    /// <exception cref="ArgumentException">The options are not valid.</exception>
    public SmtpMailSender(SmtpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var problems = options.Validate().ToList();
        if (problems.Count > 0)
            throw new ArgumentException(string.Join(" ", problems), nameof(options));

        _options = new SmtpOptions
        {
            Host = options.Host,
            Port = options.Port,
            Security = options.Security,
            Username = options.Username,
            Password = options.Password,
            Timeout = options.Timeout,
            CheckCertificateRevocation = options.CheckCertificateRevocation,
            AllowUntrustedCertificates = options.AllowUntrustedCertificates
        };
    }

    /// <inheritdoc />
    public async Task SendAsync(FluentMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var mime = message.Build();

        using var client = new SmtpClient();
        client.Timeout = (int)_options.Timeout.TotalMilliseconds;
        client.CheckCertificateRevocation = _options.CheckCertificateRevocation;
        if (_options.AllowUntrustedCertificates)
        {
            // Deliberate opt-in for trusted internal networks; off by default and documented on the option.
#pragma warning disable CA5359
            client.ServerCertificateValidationCallback = static (_, _, _, _) => true;
#pragma warning restore CA5359
        }

        await client.ConnectAsync(_options.Host, _options.Port, ToSocketOptions(_options.Security), cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);

        await client.SendAsync(mime, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(Action<FluentMessage> compose, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compose);
        using var message = new FluentMessage();
        compose(message);
        await SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private static SecureSocketOptions ToSocketOptions(SmtpSecurity security)
    {
        return security switch
        {
            SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurity.StartTlsWhenAvailable => SecureSocketOptions.StartTlsWhenAvailable,
            SmtpSecurity.None => SecureSocketOptions.None,
            _ => throw new ArgumentOutOfRangeException(nameof(security), security, null)
        };
    }
}
