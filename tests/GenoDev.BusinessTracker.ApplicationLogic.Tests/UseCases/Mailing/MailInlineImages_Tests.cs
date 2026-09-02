using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Mailing;

internal static class MailImageTestData
{
    public static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aZ1cAAAAASUVORK5CYII=");
    public static byte[] Gif => Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
    public static string Html => MailInlineImages.CreateImageHtml(Png, "Logo", 240);
}

public sealed class MailInlineImages_Tests
{
    [Theory]
    [InlineData("\"", "IMG", "SRC")]
    [InlineData("'", "img", "src")]
    [InlineData("", "img", "src")]
    public void PrepareForDelivery_ShouldPreserveMarkupAndExtractExactImageBytes(string quote, string tag, string attribute)
    {
        // Arrange
        var source = "data:image/png;base64," + Convert.ToBase64String(MailImageTestData.Png);
        var html = $"<p>Before</p><{tag} alt='Logo > text' {attribute}={quote}{source}{quote} width='240'><p>After</p>";

        // Act
        var result = MailInlineImages.PrepareForDelivery(html);

        // Assert
        var image = result.Images.Should().ContainSingle().Subject;
        image.ContentType.Should().Be("image/png");
        image.Content.Should().Equal(MailImageTestData.Png);
        result.Html.Should().Be(html.Replace(source, "cid:" + image.ContentId, StringComparison.Ordinal));
    }

    [Fact]
    public void PrepareForDelivery_ShouldReuseRepeatedImageAndLeaveOtherAttributesCommentsAndRemoteUrlsAlone()
    {
        // Arrange
        var html = MailImageTestData.Html;
        var untouched = $"<!-- {html} --><a title='{html.Replace("'", "&#39;", StringComparison.Ordinal)}'>Link</a>" +
                        "<img data-src='data:image/png;base64,invalid' src='https://example.com/logo.png'>";

        // Act
        var result = MailInlineImages.PrepareForDelivery(html + html + untouched);

        // Assert
        var image = result.Images.Should().ContainSingle().Subject;
        result.Html.Should().Contain(untouched);
        result.Html.Split("cid:" + image.ContentId).Should().HaveCount(3);
    }

    [Theory]
    [InlineData("data:image/png;base64,???", "*uszkodzone*")]
    [InlineData("data:image/png;base64,", "*pusty*")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=", "*PNG, JPG lub GIF*")]
    [InlineData("data:image/png,abc", "*Base64*")]
    [InlineData("data:image/png;base64,YWJj", "*PNG, JPG lub GIF*")]
    [InlineData("cid:missing@example", "*Wstaw obraz*")]
    [InlineData("file:///C:/logo.png", "*Wstaw obraz*")]
    [InlineData("C:\\logo.png", "*Wstaw obraz*")]
    public void Validate_ShouldRejectBrokenOrNonPortableImages(string source, string expectedError)
    {
        MailInlineImages.Validate($"<img src='{source}'>").Should().Match(expectedError);
    }

    [Fact]
    public void Validate_ShouldRejectMismatchedMimeType()
    {
        MailInlineImages.Validate(MailImageTestData.Html.Replace("image/png", "image/jpeg", StringComparison.Ordinal))
            .Should().Contain("nie zgadza się");
    }

    [Fact]
    public void Validate_ShouldEnforceImageAndCombinedAttachmentLimits()
    {
        // Arrange
        var oversized = new byte[MailInlineImages.MaxImageSizeBytes + 1];
        MailImageTestData.Png.CopyTo(oversized, 0);
        var html = "<img src='data:image/png;base64," + Convert.ToBase64String(oversized) + "'>";

        // Act / Assert
        MailInlineImages.Validate(html).Should().Contain("5 MB");
        MailInlineImages.Validate(MailImageTestData.Html, MailAttachmentConstraints.MaxTotalSizeBytes).Should().Contain("20 MB");
        MailInlineImages.Validate(MailImageTestData.Html,
            MailAttachmentConstraints.MaxTotalSizeBytes - MailImageTestData.Png.Length).Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldEnforceUniqueImageCount()
    {
        var html = string.Concat(Enumerable.Range(0, MailInlineImages.MaxImages + 1).Select(index =>
            MailInlineImages.CreateImageHtml([.. MailImageTestData.Png, (byte)index], "Logo", 100)));

        MailInlineImages.Validate(html).Should().Contain("20 różnych");
        MailInlineImages.Validate(string.Concat(Enumerable.Repeat(MailImageTestData.Html, 21))).Should().BeNull();
    }

    [Fact]
    public void CreateImageHtml_ShouldEncodeDescriptionAndKeepFilenameOutOfTemplateSyntax()
    {
        // Arrange
        var html = MailInlineImages.CreateImageHtml(MailImageTestData.Gif, "A&B \"{{> missing }}\"", 200);
        var context = new MailRenderContext(new Dictionary<string, string?>(), new Dictionary<string, IReadOnlyList<MailRenderItem>>());

        // Act
        var rendered = new MailTemplateRenderer().RenderHtml(html, new Dictionary<string, string>(), context);

        // Assert
        rendered.Should().Be(html).And.Contain("A&amp;B").And.NotContain("{{").And.Contain("width=\"200\"");
        MailInlineImages.PrepareForDelivery(rendered).Images.Should().ContainSingle().Which.Content.Should().Equal(MailImageTestData.Gif);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2001)]
    public void CreateImageHtml_ShouldRejectInvalidWidth(int width)
    {
        var action = () => MailInlineImages.CreateImageHtml(MailImageTestData.Png, "Logo", width);
        action.Should().Throw<InvalidOperationException>().WithMessage("*Szerokość*");
    }
}
