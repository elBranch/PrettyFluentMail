# PrettyFluentMail.AspNetCore

Adds dependency injection support for [PrettyFluentMail](https://github.com/elBranch/PrettyFluentMail) in ASP.NET Core applications.

## Features

- Simple registration of `SendMail` using Microsoft Dependency Injection
- Multiple configuration options (from `IConfiguration`, delegate, or direct options)

## Installation

Add the NuGet package to your project:

```
dotnet add package PrettyFluentMail.AspNetCore
```

## Usage

Register `SendMail` in your `Startup.cs` or `Program.cs`:

**Using configuration section:**
```csharp
services.AddPrettyFluentMail(Configuration.GetSection("SmtpSettings"));
```

**Using a delegate:**
```csharp
services.AddPrettyFluentMail(() => new SmtpOptions { Host = "smtp.example.com" });
```

**Using an action:**
```csharp
services.AddPrettyFluentMail(options => { options.Host = "smtp.example.com"; });
```

## Configuration Example

Add your SMTP settings to `appsettings.json`:
```json
"SmtpSettings": {
  "Host": "smtp.example.com",
  "Username": "user",
  "Password": "pass",
  "Port": 587,
  "EnableSsl": true
}
```

## License

MIT