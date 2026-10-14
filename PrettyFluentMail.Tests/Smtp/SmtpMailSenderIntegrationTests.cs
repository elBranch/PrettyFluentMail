using MailKit;
using MailKit.Security;
using MimeKit;
using PrettyFluentMail.Configuration;

namespace PrettyFluentMail.Tests.Smtp;

/// <summary>
///     Sends real SMTP traffic from <see cref="SmtpMailSender" /> to an in-process <see cref="FakeSmtpServer" />.
/// </summary>
public class SmtpMailSenderIntegrationTests
{
    private const string Mjml =
        "<mjml><mj-body><mj-section><mj-column><mj-text>Hello {{ name }}</mj-text></mj-column></mj-section></mj-body></mjml>";

    private static void Compose(FluentMessage message) => message
        .From("Sender <sender@example.com>")
        .To("to@example.com")
        .WithSubject("Integration")
        .UsingText("plain body");

    [Fact]
    public async Task Send_DeliversTemplateTextAndAttachment()
    {
        await using var server = await FakeSmtpServer.StartAsync();
        var sender = new SmtpMailSender(server.ClientOptions());

        await sender.SendAsync(message => message
            .From("Sender <sender@example.com>")
            .To("to@example.com")
            .Cc("cc@example.com")
            .WithSubject("Hello there")
            .UsingTemplateContent(Mjml, new { Name = "<Robert>" })
            .UsingTextTemplateContent("Hello {{ name }}", new { Name = "Robert" })
            .AddAttachment("report.csv", new MemoryStream("a,b"u8.ToArray()), "text/csv"));

        var received = Assert.Single(server.Messages);
        Assert.Equal("Hello there", received.Message.Subject);
        Assert.Equal("sender@example.com", received.EnvelopeFrom);
        Assert.Equal("Sender", received.Message.From.Mailboxes.Single().Name);
        Assert.Equal("Hello Robert", received.Message.TextBody!.Trim());
        Assert.Contains("Hello &lt;Robert&gt;", received.Message.HtmlBody!, StringComparison.Ordinal);
        var attachment = Assert.IsAssignableFrom<MimePart>(Assert.Single(received.Message.Attachments));
        Assert.Equal("report.csv", attachment.FileName);
        using var content = new MemoryStream();
        await attachment.Content!.DecodeToAsync(content);
        Assert.Equal("a,b", System.Text.Encoding.UTF8.GetString(content.ToArray()));
    }

    [Fact]
    public async Task Send_PreFabricatedMessage_IsNotDisposedBySender()
    {
        await using var server = await FakeSmtpServer.StartAsync();
        var sender = new SmtpMailSender(server.ClientOptions());
        using var message = new FluentMessage();
        Compose(message);

        await sender.SendAsync(message);
        await sender.SendAsync(message); // still usable, and the caller owns disposal

        Assert.Equal(2, server.Messages.Count);
    }

    [Fact]
    public async Task Bcc_IsInTheEnvelopeButNotInTheHeaders()
    {
        await using var server = await FakeSmtpServer.StartAsync();
        var sender = new SmtpMailSender(server.ClientOptions());

        await sender.SendAsync(message => { Compose(message); message.Bcc("secret@example.com"); });

        var received = Assert.Single(server.Messages);
        Assert.Contains("secret@example.com", received.EnvelopeRecipients);
        Assert.Contains("to@example.com", received.EnvelopeRecipients);
        Assert.DoesNotContain("secret@example.com", received.Message.To.ToString(), StringComparison.Ordinal);
        Assert.Empty(received.Message.Bcc);
    }

    [Fact]
    public async Task Credentials_AreSentWhenConfigured()
    {
        await using var server = await FakeSmtpServer.StartAsync(new FakeSmtpServerOptions { RequireAuthentication = true });
        var options = server.ClientOptions();
        options.Username = "user";
        options.Password = "secret";

        await new SmtpMailSender(options).SendAsync(Compose);

        Assert.Equal("user", Assert.Single(server.Messages).AuthenticatedUser);
    }

    [Fact]
    public async Task WrongPassword_IsRejected_AndNothingIsDelivered()
    {
        await using var server = await FakeSmtpServer.StartAsync(new FakeSmtpServerOptions { RequireAuthentication = true });
        var options = server.ClientOptions();
        options.Username = "user";
        options.Password = "wrong";

        await Assert.ThrowsAsync<AuthenticationException>(() => new SmtpMailSender(options).SendAsync(Compose));

        Assert.Empty(server.Messages);
    }

    [Fact]
    public async Task NoUsername_AttemptsNoAuthentication()
    {
        await using var server = await FakeSmtpServer.StartAsync();

        await new SmtpMailSender(server.ClientOptions()).SendAsync(Compose);

        Assert.Empty(server.AuthAttempts);
        Assert.Null(Assert.Single(server.Messages).AuthenticatedUser);
    }

    [Fact]
    public async Task DefaultSecurity_RefusesAServerWithoutTls_AndSendsNothing()
    {
        await using var server = await FakeSmtpServer.StartAsync(); // no TLS on offer
        var options = server.ClientOptions(SmtpSecurity.StartTls);

        await Assert.ThrowsAnyAsync<Exception>(() => new SmtpMailSender(options).SendAsync(Compose));

        Assert.Empty(server.Messages);
    }

    [Fact]
    public async Task StartTls_RejectsSelfSignedCertificate_ByDefault()
    {
        await using var server = await FakeSmtpServer.StartAsync(new FakeSmtpServerOptions { Tls = true });
        var options = server.ClientOptions(SmtpSecurity.StartTls);

        await Assert.ThrowsAsync<SslHandshakeException>(() => new SmtpMailSender(options).SendAsync(Compose));

        Assert.Empty(server.Messages);
    }

    [Fact]
    public async Task StartTls_AcceptsSelfSignedCertificate_WhenExplicitlyAllowed()
    {
        await using var server = await FakeSmtpServer.StartAsync(new FakeSmtpServerOptions { Tls = true });
        var options = server.ClientOptions(SmtpSecurity.StartTls);
        options.AllowUntrustedCertificates = true;

        await new SmtpMailSender(options).SendAsync(Compose);

        Assert.Single(server.Messages);
    }

    [Fact]
    public async Task SslOnConnect_Works_WhenExplicitlyAllowed()
    {
        await using var server = await FakeSmtpServer.StartAsync(new FakeSmtpServerOptions { Tls = true, ImplicitTls = true });
        var options = server.ClientOptions(SmtpSecurity.SslOnConnect);
        options.AllowUntrustedCertificates = true;

        await new SmtpMailSender(options).SendAsync(Compose);

        Assert.Single(server.Messages);
    }

    [Fact]
    public async Task StartTlsWhenAvailable_FallsBackToPlainText_OnAServerWithoutTls()
    {
        await using var server = await FakeSmtpServer.StartAsync();

        await new SmtpMailSender(server.ClientOptions(SmtpSecurity.StartTlsWhenAvailable)).SendAsync(Compose);

        Assert.Single(server.Messages);
    }

    [Fact]
    public async Task EachSend_UsesItsOwnConnection()
    {
        await using var server = await FakeSmtpServer.StartAsync();
        var sender = new SmtpMailSender(server.ClientOptions());

        await sender.SendAsync(Compose);
        await sender.SendAsync(Compose);

        Assert.Equal(2, server.Messages.Select(m => m.SessionId).Distinct().Count());
    }

    [Fact]
    public async Task ConcurrentSends_AllDeliver_FromOneSharedSender()
    {
        await using var server = await FakeSmtpServer.StartAsync();
        var sender = new SmtpMailSender(server.ClientOptions());

        await Task.WhenAll(Enumerable.Range(0, 10).Select(i => sender.SendAsync(message =>
        {
            Compose(message);
            message.WithSubject($"Message {i}");
        })));

        Assert.Equal(10, server.Messages.Select(m => m.Message.Subject).Distinct().Count());
    }

    [Fact]
    public async Task UnresponsiveServer_TimesOut()
    {
        await using var server = await FakeSmtpServer.StartAsync(
            new FakeSmtpServerOptions { StallBeforeAccepting = TimeSpan.FromSeconds(30) });
        var options = server.ClientOptions();
        options.Timeout = TimeSpan.FromMilliseconds(500);

        await Assert.ThrowsAnyAsync<Exception>(() => new SmtpMailSender(options).SendAsync(Compose));
        Assert.Empty(server.Messages);
    }

    [Fact]
    public async Task Cancellation_StopsASendInProgress()
    {
        await using var server = await FakeSmtpServer.StartAsync(
            new FakeSmtpServerOptions { StallBeforeAccepting = TimeSpan.FromSeconds(30) });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new SmtpMailSender(server.ClientOptions()).SendAsync(Compose, cts.Token));
    }

    [Fact]
    public async Task AttachedFile_IsReleased_AfterSending()
    {
        await using var server = await FakeSmtpServer.StartAsync();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "attached");
        try
        {
            await new SmtpMailSender(server.ClientOptions()).SendAsync(message =>
            {
                Compose(message);
                message.AddAttachment(path);
            });

            File.Delete(path); // throws if the sender left the file open
            Assert.False(File.Exists(path));
            Assert.Single(Assert.Single(server.Messages).Message.Attachments);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
