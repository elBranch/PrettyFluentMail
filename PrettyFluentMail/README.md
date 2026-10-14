# PrettyFluentMail

Compose an email with a fluent API, render its HTML body from an [MJML](https://mjml.io) template with
[Scriban](https://github.com/scriban/scriban) variables, and send it through SMTP with
[MailKit](https://github.com/jstedfast/MailKit).

- Fluent message builder: From, To, Cc, Bcc, Reply-To, priority, custom headers, attachments
- Responsive HTML from MJML, with Scriban variables, loops and conditions
- **Variables are HTML-encoded by default**, so user data cannot inject markup into your email
- Optional plain text alternative, rendered from its own Scriban template
- Secure SMTP defaults (STARTTLS, certificate validation) with explicit opt-outs for internal servers
- Async, thread-safe sender; one connection per message, nothing to manage
- Targets .NET 10

## Installation

```bash
dotnet add package PrettyFluentMail
```

For ASP.NET Core and generic host dependency injection, also add
[`PrettyFluentMail.AspNetCore`](https://www.nuget.org/packages/PrettyFluentMail.AspNetCore).

## Quick start

```csharp
using PrettyFluentMail;
using PrettyFluentMail.Configuration;

var sender = new SmtpMailSender(new SmtpOptions
{
    Host = "smtp.example.com",
    Username = "user@example.com",
    Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
});

await sender.SendAsync(message => message
    .From("Edward Lyle <edward@example.com>")
    .To("Robert Dean <robert@example.com>")
    .WithSubject("Secret surveillance")
    .UsingTemplate("Templates/welcome.mjml", new { Name = "Robert" })
    .UsingTextTemplate("Templates/welcome.txt", new { Name = "Robert" })
    .AddAttachment("report.pdf"));
```

`SendAsync(Action<FluentMessage>)` creates the message, sends it and disposes it. Each call opens a connection,
authenticates, sends and disconnects, so a single `SmtpMailSender` can be shared and called concurrently.

## SMTP options

| Option | Default | Notes |
| --- | --- | --- |
| `Host` | *(required)* | Server name or IP address. |
| `Port` | `587` | Use `465` with `SslOnConnect`. |
| `Security` | `StartTls` | See below. |
| `Username` / `Password` | `null` | Omit both for servers that do not authenticate. |
| `Timeout` | 30 seconds | Applied to each network operation. |
| `CheckCertificateRevocation` | `true` | |
| `AllowUntrustedCertificates` | `false` | Accepts self-signed, expired or mismatched certificates. |

`SmtpSecurity` values:

| Value | Behaviour |
| --- | --- |
| `StartTls` (default) | Requires STARTTLS; fails if the server does not offer it. |
| `SslOnConnect` | Implicit TLS from the first byte. |
| `StartTlsWhenAvailable` | Uses STARTTLS if offered, otherwise **sends unencrypted**. |
| `None` | **Never encrypts.** |

### Internal servers without TLS

The defaults protect credentials and content in transit. If your internal relay genuinely cannot do TLS, say so
explicitly:

```csharp
new SmtpOptions
{
    Host = "relay.corp.local",
    Port = 25,
    Security = SmtpSecurity.None,        // clear text on the wire
    AllowUntrustedCertificates = true,   // only if the relay has a self-signed certificate
};
```

Only do this on a network you trust. Prefer `StartTlsWhenAvailable` over `None` where you can, and install your
internal CA rather than setting `AllowUntrustedCertificates`.

## Composing a message

| Method | Purpose |
| --- | --- |
| `From`, `Sender` | Author and optional "Sender" header. |
| `To`, `Cc`, `Bcc` | Accept `"a@x.com"`, `"Name <a@x.com>"`, a comma-separated list, a `(displayName, address)` pair, or an `IEnumerable<string>`. |
| `ReplyTo` | Where replies should go. |
| `WithSubject`, `WithPriority`, `WithHeader` | Subject, `MessagePriority`, custom headers. |
| `UsingText`, `UsingTextTemplate`, `UsingTextTemplateContent` | Plain text body. |
| `UsingTemplate`, `UsingTemplateContent` | HTML body from MJML, from a file or from a string. |
| `AddAttachment` | A file path, or a stream with a file name. |

`Build()` validates that there is a sender, a recipient and a body, and throws `InvalidOperationException`
otherwise. After `Build()` the message can no longer be changed. Dispose a `FluentMessage` you create yourself:

```csharp
using var message = new FluentMessage()
    .From("sender@example.com")
    .To("recipient@example.com")
    .WithSubject("Hello")
    .UsingText("Hi there");

await sender.SendAsync(message);   // the caller keeps ownership
```

## Templates

An HTML body is produced in two steps: Scriban substitutes variables into the template, then MJML converts the
result to HTML.

```xml
<mjml>
  <mj-body>
    <mj-section>
      <mj-column>
        <mj-text>Hello {{ first_name }},</mj-text>
        {{ for item in items }}
        <mj-text>{{ item.title }}</mj-text>
        {{ end }}
      </mj-column>
    </mj-section>
  </mj-body>
</mjml>
```

```csharp
.UsingTemplate("welcome.mjml", new { FirstName = "Robert", Items = items })
```

- **Names are snake_case.** `FirstName` is `{{ first_name }}` in the template.
- **Only data is exposed.** Templates can read public properties and fields, not call methods on your objects.
- **Undefined variables are errors**, not blanks, so `{{ frist_name }}` fails instead of emailing an empty name.
- **No file access.** `{{ include }}` is disabled.
- **Resource limits.** Loops, recursion, regular expressions and output size are capped.
- `UsingTemplateContent(string, ...)` renders from text, for embedded resources, databases or tests.

### HTML encoding and how to opt out

Every `{{ expression }}` is HTML-encoded, so a value like `<a href="https://evil">Click</a>` is shown as text. The
literal markup of your template is never encoded. This keeps customer names, comments and other user data from
becoming links or markup in an email that arrives from your domain.

Opt out only for content you control, at the narrowest scope that works:

```
{{ welcome_banner | raw }}        <!-- one expression -->
```

```csharp
new { Footer = new RawHtml("<small>&copy; Contoso</small>") }   // one value
.UsingTemplate("newsletter.mjml", model, htmlEncode: false)      // the whole template
```

Do not combine encoding with Scriban's own `html.escape`: the value would be escaped twice. Plain text templates
(`UsingTextTemplate`) are never HTML-encoded.

### Plain text alternative

Add a plain text body next to the HTML one and the message is sent as `multipart/alternative`. It improves
spam-filter scores and works in clients that block HTML. This library does not guess a text version from your HTML,
because converted text is usually poor; write a short `.txt` template instead and give it the same model.

### Template errors

Rendering problems throw `TemplateRenderException`. `Errors` lists every problem, each with the `Stage`
(`Scriban` or `Mjml`), message, file, line and column. `InnerException` holds the engine's own exception when
there is one.

```csharp
try { message.UsingTemplate("welcome.mjml", model); }
catch (TemplateRenderException ex)
{
    foreach (var error in ex.Errors)
        logger.LogError("{Stage} {File}({Line},{Column}): {Message}",
            error.Stage, error.File, error.Line, error.Column, error.Message);
}
```

## Attachments

```csharp
.AddAttachment("reports/q3.pdf")                                      // from disk
.AddAttachment("data.csv", stream, "text/csv")                        // from memory
```

- A path is checked when you add it, opened when the message is built, and released when it is disposed.
- A stream is owned by the message from then on and is disposed with it.
- **Never pass a user-supplied path directly.** An attacker who controls the path can mail themselves any file the
  process can read. Resolve it against a fixed folder and check the result stays inside it, or use the stream overload.
- Attachments travel Base64-encoded; check your server's message size limit.

## Security checklist

- Keep passwords out of source control: use user secrets, environment variables or a secret store.
- Keep the default `Security = StartTls` unless you have a reason not to.
- Keep HTML encoding on; use `raw` only for markup you wrote.
- Treat template files as code. Anyone who can edit them controls the email content.
- Validate addresses that come from users; invalid ones throw `FormatException`.

## Upgrading from 1.x

| 1.x | 2.0 |
| --- | --- |
| `SendMail : SmtpClient` | `IMailSender` / `SmtpMailSender`, async only, built on MailKit |
| `SmtpOptions.EnableSsl` | `SmtpOptions.Security` (`StartTls` by default) |
| default port 25, no TLS | default port 587, STARTTLS required |
| `MJMLException`, `ScribanException` | `TemplateRenderException` with `Errors` and `InnerException` |
| template variables unencoded | HTML-encoded by default |
| `FluentMessage.Build()` returned `MailMessage` | returns `MimeMessage`; `FluentMessage` is `IDisposable` |
| .NET 8, 9, 10 | .NET 10 |

## License

MIT
