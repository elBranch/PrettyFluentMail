# Cackle.PrettyFluentMail.Extensions.DependencyInjection

Dependency injection for [Cackle.PrettyFluentMail](https://github.com/CackleUs/Cackle.PrettyFluentMail): one `AddPrettyFluentMail`
call registers a validated, thread-safe mail sender in ASP.NET Core, worker services and any .NET generic host.

## Installation

```bash
dotnet add package Cackle.PrettyFluentMail.Extensions.DependencyInjection
```

## Registration

The extension methods live in the `Microsoft.Extensions.DependencyInjection` namespace, so no extra `using` is needed.

**From configuration (recommended):**

```csharp
builder.Services.AddPrettyFluentMail(builder.Configuration.GetSection("Smtp"));
```

```json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Security": "StartTls",
    "Username": "user@example.com",
    "Timeout": "00:00:30"
  }
}
```

**From code:**

```csharp
builder.Services.AddPrettyFluentMail(options =>
{
    options.Host = "smtp.example.com";
    options.Password = builder.Configuration["Smtp:Password"];
});
```

Keep the password out of `appsettings.json`:

```bash
dotnet user-secrets set "Smtp:Password" "..."
```

In production use environment variables (`Smtp__Password`) or a secret store.

### Fail-fast configuration

The options are validated when the host starts. A missing `Host`, an out-of-range port, or a password without a
user name stops the application immediately. When binding from configuration, unknown keys are errors too, so a
typo such as `"Securty"` can no longer silently leave you on a default. Invalid values (for example
`"Security": "Tls-ish"`) fail as well.

### Internal servers without TLS

Defaults are secure (`StartTls`, certificate validation). To use a relay that cannot do TLS, opt out explicitly:

```json
{
  "Smtp": {
    "Host": "relay.corp.local",
    "Port": 25,
    "Security": "None",
    "AllowUntrustedCertificates": false
  }
}
```

See the [core README](https://github.com/CackleUs/Cackle.PrettyFluentMail/blob/main/Cackle.PrettyFluentMail/README.md) for every option and what it means.

## Usage

`IMailSender` is registered as a **singleton**. It keeps no connection between calls, so it is safe to inject
anywhere, including other singletons and background services.

```csharp
public sealed class WelcomeService(IMailSender mail)
{
    public Task SendAsync(User user, CancellationToken ct) =>
        mail.SendAsync(message => message
            .From("Contoso <no-reply@contoso.com>")
            .To(user.Email)
            .WithSubject("Welcome")
            .UsingTemplate("Templates/welcome.mjml", new { FirstName = user.FirstName })
            .UsingTextTemplate("Templates/welcome.txt", new { FirstName = user.FirstName }),
            ct);
}
```

Template variables are HTML-encoded by default. See the core README for templates, attachments and the
security checklist.

### Sending from a request

Sending is slow and can fail. For anything the user does not need to wait on, queue the work to a background
service (for example with a `Channel<T>` and a `BackgroundService`) instead of awaiting the SMTP call inside the request.

## Testing

Depend on `IMailSender` and substitute a fake in tests; nothing in your code needs to know about SMTP.

## License

MIT
