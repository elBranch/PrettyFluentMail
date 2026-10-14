using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MimeKit;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.ComponentModel;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace PrettyFluentMail.Tests.Smtp;

/// <summary>How a <see cref="FakeSmtpServer" /> behaves.</summary>
internal sealed class FakeSmtpServerOptions
{
    /// <summary>Offers STARTTLS (or implicit TLS, see <see cref="ImplicitTls" />) with a self-signed certificate.</summary>
    public bool Tls { get; init; }

    /// <summary>With <see cref="Tls" />, negotiate TLS immediately on connect instead of via STARTTLS.</summary>
    public bool ImplicitTls { get; init; }

    /// <summary>Require clients to authenticate with <see cref="ValidUsername" /> / <see cref="ValidPassword" />.</summary>
    public bool RequireAuthentication { get; init; }

    public string ValidUsername { get; init; } = "user";

    public string ValidPassword { get; init; } = "secret";

    /// <summary>Wait this long before accepting each message, to simulate an unresponsive server.</summary>
    public TimeSpan StallBeforeAccepting { get; init; }
}

/// <summary>A message as the fake server received it.</summary>
internal sealed record ReceivedMessage(
    MimeMessage Message,
    string EnvelopeFrom,
    IReadOnlyList<string> EnvelopeRecipients,
    string SessionId,
    string? AuthenticatedUser);

/// <summary>
///     A real SMTP server running inside the test process on a free loopback port. It needs nothing installed or
///     started outside the tests, so it works the same on a developer machine and on GitHub Actions.
/// </summary>
internal sealed class FakeSmtpServer : IAsyncDisposable
{
    private readonly SmtpServer.SmtpServer _server;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _running;
    private readonly X509Certificate2? _certificate;
    private readonly List<ReceivedMessage> _messages = [];
    private readonly List<(string User, string Password, bool Accepted)> _authAttempts = [];
    private readonly Lock _gate = new();

    private FakeSmtpServer(FakeSmtpServerOptions options)
    {
        Options = options;
        Port = GetFreePort();

        if (options.Tls)
            _certificate = CreateSelfSignedCertificate();

        var serverOptions = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Endpoint(endpoint =>
            {
                endpoint.Endpoint(new IPEndPoint(IPAddress.Loopback, Port))
                    .AllowUnsecureAuthentication() // lets the tests exercise AUTH with Security = None
                    .AuthenticationRequired(options.RequireAuthentication)
                    .IsSecure(options is { Tls: true, ImplicitTls: true });
                if (_certificate is not null)
                    endpoint.Certificate(_certificate);
            })
            .Build();

        var services = new ServiceProvider();
        services.Add(new Store(this));
        services.Add(new Authenticator(this));

        _server = new SmtpServer.SmtpServer(serverOptions, services);
        _running = _server.StartAsync(_stop.Token);
    }

    public FakeSmtpServerOptions Options { get; }

    public int Port { get; }

    /// <summary>Everything accepted so far.</summary>
    public IReadOnlyList<ReceivedMessage> Messages
    {
        get { lock (_gate) return [.. _messages]; }
    }

    /// <summary>Every AUTH attempt made by a client.</summary>
    public IReadOnlyList<(string User, string Password, bool Accepted)> AuthAttempts
    {
        get { lock (_gate) return [.. _authAttempts]; }
    }

    /// <summary>Starts a server and waits until it accepts connections.</summary>
    public static async Task<FakeSmtpServer> StartAsync(FakeSmtpServerOptions? options = null)
    {
        var server = new FakeSmtpServer(options ?? new FakeSmtpServerOptions());
        await server.WaitUntilListeningAsync();
        return server;
    }

    /// <summary>A <see cref="PrettyFluentMail.Configuration.SmtpOptions" /> pointing at this server.</summary>
    public Configuration.SmtpOptions ClientOptions(Configuration.SmtpSecurity security = Configuration.SmtpSecurity.None) =>
        new()
        {
            Host = "127.0.0.1",
            Port = Port,
            Security = security,
            Timeout = TimeSpan.FromSeconds(10)
        };

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _server.Shutdown();
        try
        {
            await _running.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            // Shutting down while a client is mid-session is expected in the timeout tests.
        }

        _stop.Dispose();
        _certificate?.Dispose();
    }

    private async Task WaitUntilListeningAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var probe = new TcpClient();
                await probe.ConnectAsync(IPAddress.Loopback, Port);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(25);
            }
        }

        throw new TimeoutException("The fake SMTP server did not start listening.");
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid("1.3.6.1.5.5.7.3.1")], critical: false));

        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        // Round-trip through PKCS#12 so the private key is usable by SslStream on every platform.
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null);
    }

    private static string Address(IMailbox mailbox) => $"{mailbox.User}@{mailbox.Host}";

    private sealed class Store(FakeSmtpServer owner) : MessageStore
    {
        public override async Task<SmtpResponse> SaveAsync(
            ISessionContext context,
            IMessageTransaction transaction,
            ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {
            if (owner.Options.StallBeforeAccepting > TimeSpan.Zero)
                await Task.Delay(owner.Options.StallBeforeAccepting, cancellationToken);

            using var stream = new MemoryStream(buffer.ToArray());
            var message = await MimeMessage.LoadAsync(stream, cancellationToken);

            var received = new ReceivedMessage(
                message,
                Address(transaction.From),
                [.. transaction.To.Select(Address)],
                context.SessionId.ToString(),
                context.Authentication.IsAuthenticated ? context.Authentication.User : null);

            lock (owner._gate) owner._messages.Add(received);
            return SmtpResponse.Ok;
        }
    }

    private sealed class Authenticator(FakeSmtpServer owner) : UserAuthenticator
    {
        public override Task<bool> AuthenticateAsync(
            ISessionContext context,
            string user,
            string password,
            CancellationToken cancellationToken)
        {
            var accepted = user == owner.Options.ValidUsername && password == owner.Options.ValidPassword;
            lock (owner._gate) owner._authAttempts.Add((user, password, accepted));
            return Task.FromResult(accepted);
        }
    }
}
