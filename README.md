# PrettyFluentMail

PrettyFluentMail is a C# library for building and sending emails using a fluent interface. It supports HTML email templates via [mjml-net](https://github.com/SebastianStehle/mjml-net) and template variables with [Scriban](https://github.com/scriban/scriban).

## Features

- Fluent API for composing emails
- MJML template support for responsive HTML emails
- Template variables with Scriban
- File attachments
- Simple SMTP configuration

## Installation

Add the package to your project (coming soon via NuGet):

```
dotnet add package PrettyFluentMail
```

Or reference the project directly.

## Usage Example

```csharp
var settings = new SmtpOptions
{
    Host = "smtp.example.com",
    Port = 587,
    EnableSsl = true,
    Username = "user@example.com",
    Password = "password"
};

var variables = new { Name = "Robert" };

var mail = new SendMail(settings);
mail.Send(msg => msg
    .From("Edward Lyle <donttrustanyone@example.com>")
    .To("Robert Dean <robert.dean@example.com>")
    .WithSubject("Secret Surveillance")
    .UsingTemplate("/var/lib/templates/email.mjml", variables)
    .AddAttachment("/home/lyle/secrets.pdf"));
```

## API Overview

### SMTP Configuration

Configure SMTP settings using `SmtpOptions`:

```csharp
var settings = new SmtpOptions
{
    Host = "smtp.example.com",
    Port = 587,
    EnableSsl = true,
    Username = "user",
    Password = "pass"
};
```

### Composing Emails

Use `FluentMessage` to build emails:

- `.From(string address)`
- `.To(string address)` / `.To(IEnumerable<string> addresses)`
- `.Cc(string address)` / `.Cc(IEnumerable<string> addresses)`
- `.Bcc(string address)` / `.Bcc(IEnumerable<string> addresses)`
- `.WithSubject(string subject)`
- `.UsingText(string body)`
- `.UsingTemplate(string templatePath, object? variables)`
- `.AddAttachment(string filePath)`

### Sending Emails

Send emails with `SendMail`:

```csharp
var mail = new SendMail(settings);
mail.Send(msg => msg
    .From("sender@example.com")
    .To("recipient@example.com")
    .WithSubject("Hello")
    .UsingText("This is a plain text email."));
```

Or send a pre-built `FluentMessage`:

```csharp
var message = new FluentMessage()
    .From("sender@example.com")
    .To("recipient@example.com")
    .WithSubject("Hi")
    .UsingText("Body");

mail.Send(message);
```

## Template Support

- MJML templates for responsive HTML emails
- Scriban for template variables

## License

MIT