using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrettyFluentMail.Configuration;

namespace PrettyFluentMail.AspNetCore;

/// <summary>
///     Provides extension methods to register <see cref="SendMail" /> with .NET Dependency Injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Registers <see cref="SendMail" /> in the service collection using configuration from an <see cref="IConfigurationSection" />.
    /// </summary>
    /// <param name="services">The service collection to add the mail service to.</param>
    /// <param name="section">The configuration section containing <see cref="SmtpOptions" /> values (Host, Username, Password, Port, EnableSsl).</param>
    /// <returns>The updated <see cref="IServiceCollection" />.</returns>
    /// <exception cref="ArgumentException">Thrown if the Host value is null or whitespace.</exception>
    public static IServiceCollection AddPrettyFluentMail(this IServiceCollection services,
        IConfigurationSection section)
    {
        var host = section["Host"];
        ArgumentException.ThrowIfNullOrWhiteSpace(host, "Host");

        var settings = new SmtpOptions
        {
            Host = host,
            Username = section["Username"],
            Password = section["Password"]
        };

        if (int.TryParse(section["Port"], out var port))
            settings.Port = port;

        if (bool.TryParse(section["EnableSsl"], out var enableSsl))
            settings.EnableSsl = enableSsl;

        return AddPrettyFluentMail(services, settings);
    }

    /// <summary>
    ///     Registers <see cref="SendMail" /> in the service collection using a delegate that returns <see cref="SmtpOptions" />.
    /// </summary>
    /// <param name="services">The service collection to add the mail service to.</param>
    /// <param name="configureMail">A delegate that returns configured <see cref="SmtpOptions" />.</param>
    /// <returns>The updated <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddPrettyFluentMail(this IServiceCollection services,
        Func<SmtpOptions> configureMail)
    {
        var smtpClientSettings = configureMail.Invoke();
        return AddPrettyFluentMail(services, smtpClientSettings);
    }

    /// <summary>
    ///     Registers <see cref="SendMail" /> in the service collection using a delegate to configure <see cref="SmtpOptions" />.
    /// </summary>
    /// <param name="services">The service collection to add the mail service to.</param>
    /// <param name="configureMail">A delegate to configure <see cref="SmtpOptions" />.</param>
    /// <returns>The updated <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddPrettyFluentMail(this IServiceCollection services,
        Action<SmtpOptions> configureMail)
    {
        var smtpClientSettings = new SmtpOptions { Host = string.Empty };
        configureMail(smtpClientSettings);
        return AddPrettyFluentMail(services, smtpClientSettings);
    }

    /// <summary>
    ///     Registers <see cref="SendMail" /> in the service collection using the provided <see cref="SmtpOptions" />.
    /// </summary>
    /// <param name="services">The service collection to add the mail service to.</param>
    /// <param name="configureMail">The configured <see cref="SmtpOptions" /> instance.</param>
    /// <returns>The updated <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddPrettyFluentMail(this IServiceCollection services, SmtpOptions configureMail)
    {
        return services.AddTransient(_ => new SendMail(configureMail));
    }
}