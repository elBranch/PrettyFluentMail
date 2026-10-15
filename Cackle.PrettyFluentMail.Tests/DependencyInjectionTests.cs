using Cackle.PrettyFluentMail.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cackle.PrettyFluentMail.Tests;

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
        using var provider = ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(new ServiceCollection()
                .AddPrettyFluentMail(Config(("Host", "smtp.example.com"), ("Port", "465"), ("Security", "SslOnConnect"),
                    ("Timeout", "00:00:10"))), validateScopes: true);

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
        using var provider = ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(new ServiceCollection()
                .AddPrettyFluentMail(o => o.Host = "smtp.example.com"));

        Assert.IsType<SmtpMailSender>(provider.GetRequiredService<IMailSender>());
    }

    [Fact]
    public void MissingHost_FailsValidation()
    {
        using var provider = ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(new ServiceCollection()
                .AddPrettyFluentMail(Config(("Port", "25"))));

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
    }

    [Fact]
    public void UnknownKey_FailsInsteadOfFallingBackToDefaults()
    {
        using var provider = ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(new ServiceCollection()
                .AddPrettyFluentMail(Config(("Host", "smtp.example.com"), ("Securty", "None"))));

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
    }

    [Fact]
    public void UnparsableValue_Fails()
    {
        using var provider = ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(new ServiceCollection()
                .AddPrettyFluentMail(Config(("Host", "smtp.example.com"), ("Security", "Tls-ish"))));

        Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
    }
}
