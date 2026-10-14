using MimeKit;

namespace PrettyFluentMail.Tests;

public class FluentMessageTests
{
    private static FluentMessage Valid() => new FluentMessage()
        .From("Sender <sender@example.com>")
        .To("recipient@example.com")
        .WithSubject("Hello")
        .UsingText("Body");

    [Fact]
    public void Build_SetsAddressesAndSubject()
    {
        using var message = Valid()
            .To(["a@example.com", "Bee <b@example.com>"])
            .Cc("cc@example.com")
            .Bcc("bcc@example.com")
            .ReplyTo("Support", "support@example.com");

        var mime = message.Build();

        Assert.Equal("sender@example.com", mime.From.Mailboxes.Single().Address);
        Assert.Equal(3, mime.To.Count);
        Assert.Single(mime.Cc);
        Assert.Single(mime.Bcc);
        Assert.Equal("Support", mime.ReplyTo.Mailboxes.Single().Name);
        Assert.Equal("Hello", mime.Subject);
    }

    [Fact]
    public void To_AcceptsCommaSeparatedList()
    {
        using var message = Valid().To("x@example.com, y@example.com");

        Assert.Equal(3, message.Build().To.Count);
    }

    [Fact]
    public void Build_PriorityAndHeader()
    {
        using var message = Valid().WithPriority(MessagePriority.Urgent).WithHeader("X-Campaign", "spring");

        var mime = message.Build();

        Assert.Equal(MessagePriority.Urgent, mime.Priority);
        Assert.Equal("spring", mime.Headers["X-Campaign"]);
    }

    [Fact]
    public void Build_WithoutSender_Throws()
    {
        using var message = new FluentMessage().To("a@example.com").UsingText("Body");

        Assert.Contains("sender", Assert.Throws<InvalidOperationException>(message.Build).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WithoutRecipient_Throws()
    {
        using var message = new FluentMessage().From("a@example.com").UsingText("Body");

        Assert.Throws<InvalidOperationException>(message.Build);
    }

    [Fact]
    public void Build_WithoutBody_Throws()
    {
        using var message = new FluentMessage().From("a@example.com").To("b@example.com");

        Assert.Throws<InvalidOperationException>(message.Build);
    }

    [Fact]
    public void Build_TextAndHtml_ProducesMultipartAlternative()
    {
        using var message = Valid().UsingTemplateContent("<mjml><mj-body><mj-section><mj-column><mj-text>Hi</mj-text></mj-column></mj-section></mj-body></mjml>");

        var body = Assert.IsType<MultipartAlternative>(message.Build().Body);

        Assert.Equal("Body", message.Build().TextBody);
        Assert.Contains("Hi", message.Build().HtmlBody, StringComparison.Ordinal);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public void Build_IsIdempotent_AndLocksTheMessage()
    {
        using var message = Valid();

        Assert.Same(message.Build(), message.Build());
        Assert.Throws<InvalidOperationException>(() => message.To("late@example.com"));
    }

    [Fact]
    public void Build_AfterDispose_Throws()
    {
        var message = Valid();
        message.Dispose();

        Assert.Throws<ObjectDisposedException>(message.Build);
    }

    [Fact]
    public void InvalidAddress_Throws()
    {
        Assert.ThrowsAny<FormatException>(() => new FluentMessage().To("not an address"));
        Assert.Throws<ArgumentException>(() => new FluentMessage().To(" "));
        Assert.Throws<ArgumentNullException>(() => new FluentMessage().To((IEnumerable<string>)null!));
    }

    [Fact]
    public void AddAttachment_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => new FluentMessage().AddAttachment("does-not-exist.pdf"));
    }

    [Fact]
    public void AddAttachment_File_IsReleasedOnDispose()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "attached");
        try
        {
            var message = Valid().AddAttachment(path);
            var mime = message.Build();

            var attachment = Assert.Single(mime.Attachments);
            Assert.Equal(Path.GetFileName(path), attachment.ContentDisposition!.FileName);

            message.Dispose();
            File.Delete(path); // throws if the handle were still open
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void AddAttachment_Stream_UsesNameAndContentType()
    {
        using var message = Valid().AddAttachment("report.csv", new MemoryStream("a,b"u8.ToArray()), "text/csv");

        var part = Assert.IsAssignableFrom<MimePart>(Assert.Single(message.Build().Attachments));

        Assert.Equal("report.csv", part.FileName);
        Assert.Equal("text/csv", part.ContentType.MimeType);
    }
}
