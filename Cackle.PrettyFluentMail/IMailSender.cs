namespace Cackle.PrettyFluentMail;

/// <summary>
///     Sends messages composed with <see cref="FluentMessage" />. Each call is self-contained: it connects,
///     authenticates, sends and disconnects, so implementations are safe to share and to call concurrently.
/// </summary>
public interface IMailSender
{
    /// <summary>
    ///     Sends a message you have composed. The caller keeps ownership and disposes the message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SendAsync(FluentMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Composes a message with the supplied delegate, sends it and disposes it.
    /// </summary>
    /// <param name="compose">A delegate that configures the message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SendAsync(Action<FluentMessage> compose, CancellationToken cancellationToken = default);
}
