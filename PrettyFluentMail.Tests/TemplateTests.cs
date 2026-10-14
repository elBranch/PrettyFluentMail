using PrettyFluentMail.Exceptions;
using PrettyFluentMail.Templates;

namespace PrettyFluentMail.Tests;

public class TemplateTests
{
    private sealed record Model(string Name, string FirstName, RawHtml Footer)
    {
        public string Shout() => Name.ToUpperInvariant();
    }

    private static string Wrap(string text) =>
        $"<mjml><mj-body><mj-section><mj-column><mj-text>{text}</mj-text></mj-column></mj-section></mj-body></mjml>";

    private static string RenderHtml(string text, object? model, bool htmlEncode = true)
    {
        using var message = new FluentMessage()
            .From("a@example.com").To("b@example.com")
            .UsingTemplateContent(Wrap(text), model, htmlEncode);
        return message.Build().HtmlBody!;
    }

    [Fact]
    public void Variables_AreSubstituted_WithSnakeCaseNames()
    {
        var html = RenderHtml("Hello {{ first_name }}", new Model("x", "Robert", default));

        Assert.Contains("Hello Robert", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Variables_AreHtmlEncodedByDefault()
    {
        var html = RenderHtml("{{ name }}", new Model("<script>alert(1)</script> & \"q\"", "f", default));

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&amp;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TemplateMarkup_IsNeverEncoded()
    {
        var html = RenderHtml("<b>bold</b> {{ name }}", new Model("n", "f", default));

        Assert.Contains("<b>bold</b>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RawFilter_OptsOutPerExpression()
    {
        var html = RenderHtml("{{ name | raw }} / {{ first_name }}", new Model("<i>trusted</i>", "<i>untrusted</i>", default));

        Assert.Contains("<i>trusted</i>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;i&gt;untrusted&lt;/i&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RawHtmlValue_OptsOutPerValue()
    {
        var html = RenderHtml("{{ footer }}", new Model("n", "f", new RawHtml("<u>footer</u>")));

        Assert.Contains("<u>footer</u>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlEncodeFalse_OptsOutPerTemplate()
    {
        var html = RenderHtml("{{ name }}", new Model("<i>x</i>", "f", default), htmlEncode: false);

        Assert.Contains("<i>x</i>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TextTemplate_IsNotHtmlEncoded()
    {
        using var message = new FluentMessage()
            .From("a@example.com").To("b@example.com")
            .UsingTextTemplateContent("Tom & Jerry: {{ name }}", new Model("A < B", "f", default));

        Assert.Equal("Tom & Jerry: A < B", message.Build().TextBody);
    }

    [Fact]
    public void UndefinedVariable_Throws()
    {
        var ex = Assert.Throws<TemplateRenderException>(() => RenderHtml("{{ nope }}", new Model("n", "f", default)));

        Assert.Equal(TemplateStage.Scriban, Assert.Single(ex.Errors).Stage);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void ParseErrors_AreAllReported()
    {
        var ex = Assert.Throws<TemplateRenderException>(() => RenderHtml("{{ if }} {{ for }}", null));

        Assert.True(ex.Errors.Count > 1);
        Assert.All(ex.Errors, error => Assert.Equal(TemplateStage.Scriban, error.Stage));
        Assert.All(ex.Errors, error => Assert.True(error.Line >= 1));
    }

    [Fact]
    public void MjmlErrors_AreReported()
    {
        var ex = Assert.Throws<TemplateRenderException>(() =>
            new FluentMessage().UsingTemplateContent("<mjml><mj-body><mj-bogus /></mj-body></mjml>"));

        Assert.NotEmpty(ex.Errors);
        Assert.All(ex.Errors, error => Assert.Equal(TemplateStage.Mjml, error.Stage));
    }

    [Fact]
    public void Templates_CannotCallMethodsOnTheModel()
    {
        Assert.ThrowsAny<Exception>(() => RenderHtml("{{ shout }}", new Model("n", "f", default)));
    }

    [Fact]
    public void Templates_CannotIncludeFiles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "secret");
        try
        {
            Assert.Throws<TemplateRenderException>(() => RenderHtml($"{{{{ include '{path.Replace('\\', '/')}' }}}}", null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RunawayLoop_IsStopped()
    {
        Assert.Throws<TemplateRenderException>(() => RenderHtml("{{ for i in 1..1000000 }}x{{ end }}", null));
    }

    [Fact]
    public void UsingTemplate_ReadsFromDisk_AndReportsFileName()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mjml");
        File.WriteAllText(path, Wrap("Hi {{ oops"));
        try
        {
            var ex = Assert.Throws<TemplateRenderException>(() => new FluentMessage().UsingTemplate(path));

            Assert.All(ex.Errors, error => Assert.Equal(path, error.File));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
