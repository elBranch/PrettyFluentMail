using PrettyFluentMail.Configuration;

namespace PrettyFluentMail.Tests;

public class SmtpOptionsTests
{
    [Fact]
    public void Defaults_AreSecure()
    {
        var options = new SmtpOptions { Host = "smtp.example.com" };

        Assert.Equal(587, options.Port);
        Assert.Equal(SmtpSecurity.StartTls, options.Security);
        Assert.True(options.CheckCertificateRevocation);
        Assert.False(options.AllowUntrustedCertificates);
        Assert.Empty(options.Validate());
    }

    [Theory]
    [InlineData("", 587, "Host")]
    [InlineData("  ", 587, "Host")]
    [InlineData("smtp.example.com", 0, "Port")]
    [InlineData("smtp.example.com", 70000, "Port")]
    public void Validate_ReportsProblems(string host, int port, string expectedMember)
    {
        var options = new SmtpOptions { Host = host, Port = port };

        Assert.Contains(options.Validate(), problem => problem.StartsWith(expectedMember, StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsPasswordWithoutUsername()
    {
        var options = new SmtpOptions { Host = "smtp.example.com", Password = "secret" };

        Assert.Contains(options.Validate(), problem => problem.StartsWith("Username", StringComparison.Ordinal));
    }

    [Fact]
    public void Sender_RejectsInvalidOptions()
    {
        Assert.Throws<ArgumentException>(() => new SmtpMailSender(new SmtpOptions { Host = "" }));
    }

    [Fact]
    public async Task Sender_RejectsIncompleteMessageBeforeConnecting()
    {
        var sender = new SmtpMailSender(new SmtpOptions { Host = "invalid.invalid" });
        using var message = new FluentMessage().From("a@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(message));
    }
}
