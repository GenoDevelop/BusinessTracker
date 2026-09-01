using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Mailing;

public sealed class MailTemplateRenderer_Tests
{
    private readonly MailTemplateRenderer _sut = new();

    [Fact]
    public void RenderHtml_ShouldExpandSnippetsConditionsCollectionsAndEncodeValues()
    {
        var context = new MailRenderContext(
            new Dictionary<string, string?> { ["client.name"] = "A&B", ["order.trackingNumber"] = "123" },
            new Dictionary<string, IReadOnlyList<MailRenderItem>>
            {
                ["order.products"] = [new MailRenderItem(new Dictionary<string, string?> { ["product.name"] = "<Produkt>" })]
            });

        var result = _sut.RenderHtml("<h1>{{ client.name }}</h1>{{#if order.trackingNumber}}OK{{/if}}{{#each order.products}}<p>{{ product.name }}</p>{{/each}}{{> footer }}",
            new Dictionary<string, string> { ["footer"] = "<footer>{{ client.name }}</footer>" }, context);

        result.Should().Be("<h1>A&amp;B</h1>OK<p>&lt;Produkt&gt;</p><footer>A&amp;B</footer>");
    }

    [Fact]
    public void RenderSubject_ShouldRejectSnippet()
    {
        var context = new MailRenderContext(new Dictionary<string, string?>(), new Dictionary<string, IReadOnlyList<MailRenderItem>>());
        var action = () => _sut.RenderSubject("{{> footer }}", context);
        action.Should().Throw<InvalidOperationException>().WithMessage("*wyłącznie zmienne*");
    }

    [Fact]
    public void RenderHtml_ShouldExpandNestedSnippets()
    {
        var context = new MailRenderContext(
            new Dictionary<string, string?> { ["client.name"] = "Jan & Syn" },
            new Dictionary<string, IReadOnlyList<MailRenderItem>>());
        var snippets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["layout"] = "<main>{{> greeting }}</main>",
            ["greeting"] = "<p>Dzień dobry {{ client.name }}</p>"
        };

        var result = _sut.RenderHtml("{{> layout }}", snippets, context);

        result.Should().Be("<main><p>Dzień dobry Jan &amp; Syn</p></main>");
    }

    [Fact]
    public void RenderHtml_ShouldRejectNestedSnippetCycleAndShowItsPath()
    {
        var context = new MailRenderContext(
            new Dictionary<string, string?>(),
            new Dictionary<string, IReadOnlyList<MailRenderItem>>());
        var snippets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["header"] = "{{> address }}",
            ["address"] = "{{> header }}"
        };

        var action = () => _sut.RenderHtml("{{> header }}", snippets, context);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*header → address → header*");
    }
}
