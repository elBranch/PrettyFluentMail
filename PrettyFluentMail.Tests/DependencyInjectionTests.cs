using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PrettyFluentMail.Configuration;

namespace PrettyFluentMail.Tests;

public class DependencyInjectionTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => $"Smtp:{v.Key}", v => (string?)v.Value))
            .Build()
            .GetSection("Smtp");

    [Fact]
    public void FromConfiguration_BindsOptions_AndRegistersSingletonSender()
    {
        using var provider = new ServiceCollection()
            .AddPrettyFluentMail(Config(("Host", "smtp.example.com"), ("Port", "465"), ("Security", "SslOnConnect"),
                ("Timeout", "00:00:10")))
            .BuildServiceProvider(validateScopes: true);

        var options = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

        Assert.Equal("smtp.example.com", options.Host);
        Assert.Equal(465, options.Port);
        Assert.Equal(SmtpSecurity.SslOnConnect, options.Security);
        Assert.Equal(TimeSpan.FromSeconds(10), options.Timeout);
        Assert.Same(provider.GetRequiredService<IMailSender>(), provider.GetRequiredService<IMailSender>());
    }

    [Fact]
    public void FromDelegate_RegistersSender()
    {
        using var provider = new ServiceCollection()
            .AddPrettyFluentMail(o => o.Host = "smtp.example.com")
            .BuildServiceProvider();

        Assert.IsType<SmtpMailSender>(provider.GetRequiredService<IMailSender>());
    }

    [Fact]
    public void MissingHost_FailsValidation()
    {
        using var provider = new ServiceCollection()
            .AddPrettyFluentMail(Config(("Port", "25")))
            .BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
    }

    [Fact]
    public void UnknownKey_FailsInsteadOfFallingBackToDefaults()
    {
        using var provider = new ServiceCollection()
            .AddPrettyFluentMail(Config(("Host", "smtp.example.com"), ("Securty", "None")))
            .BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
    }

    [Fact]
    public void UnparsableValue_Fails()
    {
        using var provider = new ServiceCollection()
            .AddPrettyFluentMail(Config(("Host", "smtp.example.com"), ("Security", "Tls-ish")))
            .BuildServiceProvider();

        Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
    }
}
