using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;

namespace GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

public interface IMailTemplateRenderer
{
    string RenderSubject(string template, MailRenderContext context);
    string RenderHtml(string template, IReadOnlyDictionary<string, string> snippets, MailRenderContext context);
}
