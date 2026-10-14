using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PrettyFluentMail;
using PrettyFluentMail.Configuration;

// Placed in the DI namespace so the extension methods are found without an extra using directive.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Registers PrettyFluentMail with the .NET dependency injection container.
/// </summary>
public static class PrettyFluentMailServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers <see cref="IMailSender" /> using <see cref="SmtpOptions" /> bound from configuration.
        ///     The options are validated when the host starts, so a bad configuration fails fast.
        /// </summary>
        /// <param name="configuration">
        ///     The configuration section holding the <see cref="SmtpOptions" /> values, for example
        ///     <c>configuration.GetSection("Smtp")</c>.
        /// </param>
        /// <returns>The same <paramref name="services" /> instance, for chaining.</returns>
        public IServiceCollection AddPrettyFluentMail(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            // ErrorOnUnknownConfiguration turns a misspelled key (for example "Securty") into a startup error
            // instead of a silent fall back to a default.
            AddCore(services).Bind(configuration, static binder => binder.ErrorOnUnknownConfiguration = true);
            return services;
        }

        /// <summary>
        ///     Registers <see cref="IMailSender" /> using <see cref="SmtpOptions" /> configured by a delegate.
        ///     The options are validated when the host starts, so a bad configuration fails fast.
        /// </summary>
        /// <param name="configure">A delegate that configures the options.</param>
        /// <returns>The same <paramref name="services" /> instance, for chaining.</returns>
        public IServiceCollection AddPrettyFluentMail(Action<SmtpOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            AddCore(services).Configure(configure);
            return services;
        }
    }

    private static OptionsBuilder<SmtpOptions> AddCore(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>());

        // The sender holds no connection between calls, so a single shared instance is correct.
        services.TryAddSingleton<IMailSender>(static provider =>
            new SmtpMailSender(provider.GetRequiredService<IOptions<SmtpOptions>>().Value));

        return services.AddOptions<SmtpOptions>().ValidateOnStart();
    }

    private sealed class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmtpOptions options)
        {
            var problems = options.Validate().ToList();
            return problems.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(problems);
        }
    }
}
