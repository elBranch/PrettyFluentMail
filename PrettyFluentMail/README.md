# PrettyFluentMail
Easily creates HTML based emails using templates thanks to [mjml-net](https://github.com/SebastianStehle/mjml-net) complete with template variables thanks to [scriban](https://github.com/scriban/scriban).

**Usage Example**
```cs
var settings = new SmtpClientSettings();
var variables = new {Name = "Robert"};
var mail = new SendMail(settings);
mail.Send(msg => msg
    .From("Edward Lyle <donttrustanyone@example.com>")
    .To("Robert Dean <robert.dean@example.com>")
    .WithSubject("Secret Surveillance")
    .UsingTemplate("/var/lib/templates/email.mjml", variables)
    .AddAttachment("/home/lyle/secrets.pdf"));
```
