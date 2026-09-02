using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.DeleteMailingItem;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailingWorkspace;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetOutgoingEmailHistory;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.RenderMailPreview;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailSnippet;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailTemplate;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveSmtpAccount;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;
using GenoDev.BusinessTracker.Wpf.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class MailingViewModel(IMediator mediator, IServiceProvider serviceProvider) : ViewModelBase
{
    private CancellationTokenSource? _templatePreviewCancellation;
    private CancellationTokenSource? _snippetPreviewCancellation;

    public ObservableCollection<SmtpAccountDto> Accounts { get; } = [];
    public ObservableCollection<MailSnippetDto> Snippets { get; } = [];
    public ObservableCollection<MailTemplateDto> Templates { get; } = [];
    public ObservableCollection<MailAttachmentEditorItem> TemplateAttachments { get; } = [];
    public ObservableCollection<OutgoingEmailListDto> History { get; } = [];
    public ObservableCollection<MailPreviewOrderOption> PreviewOrders { get; } = [];
    public PaginationPageLoader HistoryPageLoader => LoadHistoryPageAsync;
    public event Action? HistoryRefreshRequested;

    public IReadOnlyList<MailVariableCategory> VariableCategories { get; } = MailTokenCatalog.VariableCategories;
    public IReadOnlyList<MailTokenDto> Conditions { get; } = MailTokenCatalog.Conditions;
    public IReadOnlyList<MailTokenDto> Loops { get; } = MailTokenCatalog.Loops;

    [ObservableProperty] private SmtpAccountDto? _selectedAccount;
    [ObservableProperty] private Guid? _accountId;
    [ObservableProperty] private string _accountName = string.Empty;
    [ObservableProperty] private string _smtpHost = "smtp.gmail.com";
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private bool _useStartTls = true;
    [ObservableProperty] private string _smtpUserName = string.Empty;
    [ObservableProperty] private string _smtpPassword = string.Empty;
    [ObservableProperty] private string _fromAddress = string.Empty;
    [ObservableProperty] private string _fromName = string.Empty;
    [ObservableProperty] private string? _replyToAddress;
    [ObservableProperty] private bool _isDefaultAccount;
    [ObservableProperty] private bool _isAccountEnabled = true;

    [ObservableProperty] private MailSnippetDto? _selectedSnippet;
    [ObservableProperty] private Guid? _snippetId;
    [ObservableProperty] private string _snippetKey = string.Empty;
    [ObservableProperty] private string _snippetName = string.Empty;
    [ObservableProperty] private string? _snippetDescription;
    [ObservableProperty] private string _snippetHtml = "<p></p>";
    [ObservableProperty] private bool _isSnippetActive = true;

    [ObservableProperty] private MailTemplateDto? _selectedTemplate;
    [ObservableProperty] private Guid? _templateId;
    [ObservableProperty] private string _templateName = string.Empty;
    [ObservableProperty] private SmtpAccountDto? _templateAccount;
    [ObservableProperty] private string _subjectTemplate = string.Empty;
    [ObservableProperty] private string _htmlTemplate = "<p>Dzień dobry {{ client.name }},</p>";
    [ObservableProperty] private bool _isTemplateActive = true;
    [ObservableProperty] private MailPreviewOrderOption? _selectedPreviewOrder;
    [ObservableProperty] private string _templatePreviewHtml = "<p>Dzień dobry {{ client.name }},</p>";
    [ObservableProperty] private string _snippetPreviewHtml = "<p></p>";
    [ObservableProperty] private string? _templateAttachmentErrorMessage;

    [ObservableProperty] private OutgoingEmailListDto? _selectedHistoryItem;
    [ObservableProperty] private MailComposerViewModel? _resendComposer;
    [ObservableProperty] private bool _isResendOpen;
    [ObservableProperty] private bool _isDeleteConfirmationOpen;
    [ObservableProperty] private string _deleteConfirmationTitle = string.Empty;
    [ObservableProperty] private string _deleteConfirmationItemName = string.Empty;
    private Guid? _itemIdPendingDelete;
    private MailingItemKind? _itemKindPendingDelete;

    public async Task LoadAsync()
    {
        await YieldToUiAsync();
        var selectedAccountId = AccountId;
        var selectedSnippetId = SnippetId;
        var selectedTemplateId = TemplateId;
        var unsavedTemplateAccountId = TemplateAccount?.Id;
        var workspace = await mediator.Send(new GetMailingWorkspaceQuery());
        Replace(Accounts, workspace.Accounts);
        Replace(Snippets, workspace.Snippets);
        Replace(Templates, workspace.Templates);

        SelectedAccount = null;
        SelectedAccount = Accounts.FirstOrDefault(x => x.Id == selectedAccountId);
        SelectedSnippet = null;
        SelectedSnippet = Snippets.FirstOrDefault(x => x.Id == selectedSnippetId);
        SelectedTemplate = null;
        SelectedTemplate = Templates.FirstOrDefault(x => x.Id == selectedTemplateId);
        if (selectedTemplateId is null && unsavedTemplateAccountId is not null)
        {
            TemplateAccount = Accounts.FirstOrDefault(x => x.Id == unsavedTemplateAccountId);
        }

        var selectedOrderId = SelectedPreviewOrder?.Id;
        PreviewOrders.Clear();
        PreviewOrders.Add(MailPreviewOrderOption.None);
        var pageIndex = 0;
        bool hasNextPage;
        do
        {
            var orders = await mediator.Send(new GetOrdersQuery(pageIndex, 1000));
            foreach (var order in orders.Items)
            {
                var identifier = string.IsNullOrWhiteSpace(order.OrderIdentifier)
                    ? order.Id.ToString("N")[..8]
                    : order.OrderIdentifier;
                var clientName = string.IsNullOrWhiteSpace(order.ClientDetails?.ClientName)
                    ? "bez nazwy klienta"
                    : order.ClientDetails.ClientName;
                PreviewOrders.Add(new MailPreviewOrderOption(order.Id,
                    $"{identifier} — {order.OrderDate:dd.MM.yyyy} — {clientName}"));
            }

            hasNextPage = orders.HasNextPage;
            pageIndex++;
        }
        while (hasNextPage);

        var previewOrder = PreviewOrders.FirstOrDefault(x => x.Id == selectedOrderId) ?? PreviewOrders[0];
        SelectedPreviewOrder = null;
        SelectedPreviewOrder = previewOrder;
    }

    partial void OnSelectedPreviewOrderChanged(MailPreviewOrderOption? value)
    {
        QueueTemplatePreview();
        QueueSnippetPreview();
    }

    partial void OnHtmlTemplateChanged(string value) => QueueTemplatePreview();

    partial void OnSnippetHtmlChanged(string value) => QueueSnippetPreview();

    partial void OnTemplateAccountChanged(SmtpAccountDto? value) => QueueTemplatePreview();

    partial void OnSelectedAccountChanged(SmtpAccountDto? value)
    {
        if (value is null) return;
        AccountId = value.Id; AccountName = value.Name; SmtpHost = value.Host; SmtpPort = value.Port;
        UseStartTls = value.UseStartTls; SmtpUserName = value.UserName; SmtpPassword = string.Empty;
        FromAddress = value.FromAddress; FromName = value.FromName; ReplyToAddress = value.ReplyToAddress;
        IsDefaultAccount = value.IsDefault; IsAccountEnabled = value.IsEnabled;
    }

    partial void OnSelectedSnippetChanged(MailSnippetDto? value)
    {
        if (value is null) return;
        SnippetId = value.Id; SnippetKey = value.Key; SnippetName = value.Name;
        SnippetDescription = value.Description; SnippetHtml = value.HtmlContent; IsSnippetActive = value.IsActive;
    }

    partial void OnSelectedTemplateChanged(MailTemplateDto? value)
    {
        if (value is null) return;
        TemplateAttachmentErrorMessage = null;
        TemplateId = value.Id; TemplateName = value.Name; SubjectTemplate = value.SubjectTemplate;
        HtmlTemplate = value.HtmlTemplate; IsTemplateActive = value.IsActive;
        TemplateAccount = Accounts.FirstOrDefault(x => x.Id == value.SmtpAccountId);
        TemplateAttachments.Clear();
        foreach (var attachment in value.Attachments.Where(x => x.Content is not null))
            TemplateAttachments.Add(new MailAttachmentEditorItem { Id = attachment.Id, TemplateAttachmentId = attachment.Id,
                FileName = attachment.FileName, ContentType = attachment.ContentType, Content = attachment.Content!, Sha256 = attachment.Sha256 });
    }

    [RelayCommand] private void NewAccount()
    {
        SelectedAccount = null; AccountId = null; AccountName = string.Empty; SmtpHost = "smtp.gmail.com";
        SmtpPort = 587; UseStartTls = true; SmtpUserName = string.Empty; SmtpPassword = string.Empty;
        FromAddress = string.Empty; FromName = string.Empty; ReplyToAddress = null; IsDefaultAccount = Accounts.Count == 0; IsAccountEnabled = true;
    }

    [RelayCommand] private async Task SaveAccount()
    {
        await ExecuteSaveAsync(MailingItemKind.SmtpAccount, async () => await mediator.Send(new SaveSmtpAccountCommand(AccountId, AccountName, SmtpHost, SmtpPort,
            UseStartTls, SmtpUserName, SmtpPassword, FromAddress, FromName, ReplyToAddress, IsDefaultAccount, IsAccountEnabled)));
    }

    [RelayCommand] private void DeleteAccount()
    {
        if (AccountId is null) return;
        OpenDeleteConfirmation(AccountId.Value, MailingItemKind.SmtpAccount, "Usuń konto nadawcze", AccountName);
    }

    [RelayCommand] private void NewSnippet()
    {
        SelectedSnippet = null; SnippetId = null; SnippetKey = string.Empty; SnippetName = string.Empty;
        SnippetDescription = null; SnippetHtml = "<p></p>"; IsSnippetActive = true;
    }

    [RelayCommand] private async Task SaveSnippet()
    {
        await ExecuteSaveAsync(MailingItemKind.Snippet, async () => await mediator.Send(new SaveMailSnippetCommand(SnippetId, SnippetKey, SnippetName,
            SnippetDescription, SnippetHtml, IsSnippetActive)));
    }

    [RelayCommand] private void DeleteSnippet()
    {
        if (SnippetId is null) return;
        OpenDeleteConfirmation(SnippetId.Value, MailingItemKind.Snippet, "Usuń snippet", SnippetName);
    }

    [RelayCommand] private void NewTemplate()
    {
        SelectedTemplate = null; TemplateId = null; TemplateName = string.Empty; TemplateAccount = Accounts.FirstOrDefault(x => x.IsDefault);
        SubjectTemplate = string.Empty; HtmlTemplate = "<p>Dzień dobry {{ client.name }},</p>"; IsTemplateActive = true;
        TemplateAttachments.Clear(); TemplateAttachmentErrorMessage = null;
    }

    [RelayCommand] private async Task AddTemplateAttachments()
    {
        var filePaths = MailFileHelpers.SelectAttachmentFiles("Dodaj załączniki do szablonu");
        if (filePaths is null) return;

        TemplateAttachmentErrorMessage = null;
        IsBusy = true;
        try
        {
            var loadedAttachments = await MailFileHelpers.LoadAttachmentsAsync(TemplateAttachments, filePaths);
            foreach (var attachment in loadedAttachments)
            {
                TemplateAttachments.Add(attachment);
            }
        }
        catch (RequestValidationException exception)
        {
            TemplateAttachmentErrorMessage = string.Join(
                Environment.NewLine,
                exception.Errors.Select(error => error.Message).Distinct());
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private void RemoveTemplateAttachment(MailAttachmentEditorItem? item)
    {
        if (item is not null) TemplateAttachments.Remove(item);
    }

    [RelayCommand]
    private async Task DownloadTemplateAttachment(MailAttachmentEditorItem? item)
    {
        if (item is null) return;
        TemplateAttachmentErrorMessage = null;
        try
        {
            await MailFileHelpers.SaveAttachmentAsync(item.FileName, item.Content);
        }
        catch (RequestValidationException exception)
        {
            TemplateAttachmentErrorMessage = string.Join(Environment.NewLine, exception.Errors.Select(x => x.Message).Distinct());
        }
    }

    [RelayCommand] private async Task SaveTemplate()
    {
        var attachments = TemplateAttachments.Select(x => new MailAttachmentInput(x.Id, x.FileName, x.ContentType, x.Content, x.TemplateAttachmentId)).ToList();
        await ExecuteSaveAsync(MailingItemKind.Template, async () => await mediator.Send(new SaveMailTemplateCommand(TemplateId, TemplateAccount?.Id,
            TemplateName, SubjectTemplate, HtmlTemplate, IsTemplateActive, attachments)));
    }

    [RelayCommand] private void DeleteTemplate()
    {
        if (TemplateId is null) return;
        OpenDeleteConfirmation(TemplateId.Value, MailingItemKind.Template, "Usuń szablon", TemplateName);
    }

    [RelayCommand] private async Task RefreshMailingWorkspace() => await LoadAsync();

    [RelayCommand] private async Task Resend()
    {
        if (SelectedHistoryItem is null) return;
        if (ResendComposer is { } previousComposer)
        {
            previousComposer.RequestClose -= ResendComposer_RequestClose;
        }

        var composer = ActivatorUtilities.CreateInstance<MailComposerViewModel>(serviceProvider, SelectedHistoryItem.Id, true);
        composer.RequestClose += ResendComposer_RequestClose;
        ResendComposer = composer; IsResendOpen = true; RequestPopupOpen(nameof(IsResendOpen));
        await composer.InitializeAsync();
    }

    private void ResendComposer_RequestClose(object? sender, EventArgs e)
    {
        if (sender is MailComposerViewModel composer)
        {
            composer.RequestClose -= ResendComposer_RequestClose;
        }

        IsResendOpen = false;
        ResendComposer = null;
        HistoryRefreshRequested?.Invoke();
    }

    private async Task<int> LoadHistoryPageAsync(PaginationState state, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOutgoingEmailHistoryQuery(state.PageIndex, state.PageSize), cancellationToken);
        Replace(History, result.Items);
        return result.TotalCount;
    }

    private async Task ExecuteSaveAsync(MailingItemKind kind, Func<Task<Guid>> action)
    {
        ClearValidationErrors(); IsBusy = true;
        try
        {
            var savedId = await action();
            switch (kind)
            {
                case MailingItemKind.SmtpAccount: AccountId = savedId; break;
                case MailingItemKind.Snippet: SnippetId = savedId; break;
                case MailingItemKind.Template: TemplateId = savedId; break;
            }

            await LoadAsync();
        }
        catch (RequestValidationException exception) { ApplyValidationErrors(exception); }
        finally { IsBusy = false; }
    }

    private void OpenDeleteConfirmation(Guid itemId, MailingItemKind kind, string title, string itemName)
    {
        _itemIdPendingDelete = itemId;
        _itemKindPendingDelete = kind;
        DeleteConfirmationTitle = title;
        DeleteConfirmationItemName = itemName;
        IsDeleteConfirmationOpen = true;
        RequestPopupOpen(nameof(IsDeleteConfirmationOpen));
    }

    [RelayCommand]
    private async Task ConfirmDelete()
    {
        if (_itemIdPendingDelete is not { } itemId || _itemKindPendingDelete is not { } kind) return;
        ClearValidationErrors(); IsBusy = true;
        try
        {
            await mediator.Send(new DeleteMailingItemCommand(itemId, kind));
            switch (kind)
            {
                case MailingItemKind.SmtpAccount: NewAccount(); break;
                case MailingItemKind.Snippet: NewSnippet(); break;
                case MailingItemKind.Template: NewTemplate(); break;
            }

            IsDeleteConfirmationOpen = false;
            ClearDeleteTarget();
            await LoadAsync();
        }
        catch (RequestValidationException exception) { ApplyValidationErrors(exception); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteConfirmationOpen = false;
        ClearDeleteTarget();
    }

    private void ClearDeleteTarget()
    {
        _itemIdPendingDelete = null;
        _itemKindPendingDelete = null;
        DeleteConfirmationTitle = string.Empty;
        DeleteConfirmationItemName = string.Empty;
    }

    private void QueueTemplatePreview()
    {
        _templatePreviewCancellation?.Cancel();
        _templatePreviewCancellation?.Dispose();
        if (SelectedPreviewOrder?.Id is not { } orderId)
        {
            TemplatePreviewHtml = HtmlTemplate;
            _templatePreviewCancellation = null;
            return;
        }

        _templatePreviewCancellation = new CancellationTokenSource();
        _ = RenderTemplatePreviewAsync(orderId, _templatePreviewCancellation.Token);
    }

    private void QueueSnippetPreview()
    {
        _snippetPreviewCancellation?.Cancel();
        _snippetPreviewCancellation?.Dispose();
        if (SelectedPreviewOrder?.Id is not { } orderId)
        {
            SnippetPreviewHtml = SnippetHtml;
            _snippetPreviewCancellation = null;
            return;
        }

        _snippetPreviewCancellation = new CancellationTokenSource();
        _ = RenderSnippetPreviewAsync(orderId, _snippetPreviewCancellation.Token);
    }

    private async Task RenderTemplatePreviewAsync(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            TemplatePreviewHtml = await mediator.Send(
                new RenderMailPreviewQuery(orderId, TemplateAccount?.Id, HtmlTemplate), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception) { TemplatePreviewHtml = HtmlTemplate; }
    }

    private async Task RenderSnippetPreviewAsync(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            SnippetPreviewHtml = await mediator.Send(
                new RenderMailPreviewQuery(orderId, null, SnippetHtml), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception) { SnippetPreviewHtml = SnippetHtml; }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear(); foreach (var value in values) target.Add(value);
    }
}

public sealed record MailPreviewOrderOption(Guid? Id, string DisplayName)
{
    public static MailPreviewOrderOption None { get; } = new(null, "Bez zamówienia — surowy HTML");

    public override string ToString() => DisplayName;
}

public sealed record MailVariableCategory(string Name, IReadOnlyList<MailTokenDto> Variables);

public static class MailTokenCatalog
{
    public static IReadOnlyList<MailVariableCategory> VariableCategories { get; } =
    [
        new("Zamówienie",
        [
            new("{{ order.id }}", "Techniczny identyfikator zamówienia", "Zmienne", "Identyfikator GUID zamówienia"),
            new("{{ order.identifier }}", "Numer zamówienia", "Zmienne", "Czytelny identyfikator zamówienia"),
            new("{{ order.orderDate }}", "Data zamówienia", "Zmienne", "Data w formacie dzień.miesiąc.rok"),
            new("{{ order.status }}", "Status zamówienia", "Zmienne", "Aktualny status zamówienia"),
            new("{{ order.source }}", "Źródło zamówienia", "Zmienne", "Źródło, z którego pochodzi zamówienie"),
            new("{{ order.description }}", "Opis zamówienia", "Zmienne", "Dodatkowy opis zamówienia"),
        ]),
        new("Wysyłka",
        [
            new("{{ order.trackingNumber }}", "Numer przesyłki", "Zmienne", "Numer śledzenia przesyłki"),
            new("{{ order.trackingUrl }}", "URL do śledzenia przesyłki", "Zmienne", "Link na podstawie przewoźnika i numeru przesyłki, jak w szczegółach zamówienia; pusty przy braku danych lub obsługi przewoźnika"),
            new("{{ order.carrier }}", "Przewoźnik", "Zmienne", "Przewoźnik przypisany do zamówienia"),
            new("{{ order.shippingNetClientPrice }}", "Koszt wysyłki netto", "Zmienne", "Koszt wysyłki netto dla klienta"),
            new("{{ order.shippingGrossClientPrice }}", "Koszt wysyłki brutto", "Zmienne", "Koszt wysyłki brutto dla klienta"),
        ]),
        new("Płatności",
        [
            new("{{ order.paymentIdentifier }}", "Identyfikator płatności", "Zmienne", "Identyfikator używany przy płatności"),
            new("{{ order.totalNetPrice }}", "Łączna wartość netto", "Zmienne", "Wartość produktów i wysyłki netto"),
            new("{{ order.totalGrossPrice }}", "Łączna wartość brutto", "Zmienne", "Wartość produktów i wysyłki brutto"),
        ]),
        new("Klient",
        [
            new("{{ client.name }}", "Nazwa klienta", "Zmienne", "Imię, nazwisko lub nazwa klienta"),
            new("{{ client.email }}", "E-mail klienta", "Zmienne", "Adres e-mail klienta"),
            new("{{ client.phone }}", "Telefon klienta", "Zmienne", "Numer telefonu klienta"),
            new("{{ client.street }}", "Ulica klienta", "Zmienne", "Ulica i numer klienta"),
            new("{{ client.postCode }}", "Kod pocztowy klienta", "Zmienne", "Kod pocztowy klienta"),
            new("{{ client.city }}", "Miasto klienta", "Zmienne", "Miasto klienta"),
            new("{{ client.description }}", "Opis klienta", "Zmienne", "Dodatkowy opis klienta"),
        ]),
        new("Produkty",
        [
            new("{{ product.name }}", "Nazwa produktu", "Zmienne", "Dostępna wewnątrz pętli produktów"),
            new("{{ product.identifier }}", "Identyfikator produktu", "Zmienne", "Dostępny wewnątrz pętli produktów"),
            new("{{ product.orderedAmount }}", "Zamówiona ilość produktu", "Zmienne", "Dostępna wewnątrz pętli produktów"),
            new("{{ product.assignedAmount }}", "Przypisana ilość produktu", "Zmienne", "Dostępna wewnątrz pętli produktów"),
            new("{{ product.unitNetPrice }}", "Cena jednostkowa netto produktu", "Zmienne", "Dostępna wewnątrz pętli produktów"),
            new("{{ product.unitGrossPrice }}", "Cena jednostkowa brutto produktu", "Zmienne", "Dostępna wewnątrz pętli produktów"),
            new("{{ product.totalNetPrice }}", "Łączna cena netto produktu", "Zmienne", "Dostępna wewnątrz pętli produktów"),
            new("{{ product.totalGrossPrice }}", "Łączna cena brutto produktu", "Zmienne", "Dostępna wewnątrz pętli produktów"),
        ]),
        new("Materiały pakowe",
        [
            new("{{ packingMaterial.name }}", "Nazwa materiału pakowego", "Zmienne", "Dostępna wewnątrz pętli materiałów pakowych"),
            new("{{ packingMaterial.amount }}", "Ilość materiału pakowego", "Zmienne", "Dostępna wewnątrz pętli materiałów pakowych"),
            new("{{ packingMaterial.unit }}", "Jednostka materiału pakowego", "Zmienne", "Dostępna wewnątrz pętli materiałów pakowych")
        ]),
        new("Nadawca",
        [
            new("{{ sender.name }}", "Nazwa nadawcy", "Zmienne", "Nazwa nadawcy z wybranego konta SMTP"),
            new("{{ sender.email }}", "E-mail nadawcy", "Zmienne", "Adres nadawcy z wybranego konta SMTP"),
        ])
    ];

    public static IReadOnlyList<MailTokenDto> Conditions { get; } =
    [
        new("{{#if client.isCompany}}\n    ...\n{{/if}}", "Klient jest firmą", "Warunki", "Wyświetla zawartość, gdy w zamówieniu zaznaczono pole Firma"),
        new("{{#if client.isNotCompany}}\n    ...\n{{/if}}", "Klient nie jest firmą", "Warunki", "Wyświetla zawartość, gdy w zamówieniu nie zaznaczono pola Firma"),
        new("{{#if order.trackingNumber}}\n    ...\n{{/if}}", "Jeżeli istnieje numer przesyłki", "Warunki", "Wyświetla zawartość tylko po przypisaniu numeru śledzenia"),
        new("{{#if order.carrier}}\n    ...\n{{/if}}", "Jeżeli wybrano przewoźnika", "Warunki", "Wyświetla zawartość tylko po wybraniu przewoźnika"),
        new("{{#if order.description}}\n    ...\n{{/if}}", "Jeżeli zamówienie ma opis", "Warunki", "Wyświetla zawartość tylko dla zamówienia z opisem"),
        new("{{#if client.email}}\n    ...\n{{/if}}", "Jeżeli klient ma adres e-mail", "Warunki", "Wyświetla zawartość tylko wtedy, gdy adres e-mail jest dostępny"),
        new("{{#if client.phone}}\n    ...\n{{/if}}", "Jeżeli klient ma telefon", "Warunki", "Wyświetla zawartość tylko wtedy, gdy numer telefonu jest dostępny"),
        new("{{#if client.description}}\n    ...\n{{/if}}", "Jeżeli klient ma opis", "Warunki", "Wyświetla zawartość tylko dla klienta z opisem")
    ];

    public static IReadOnlyList<MailTokenDto> Loops { get; } =
    [
        new("{{#each order.products}}\n    <p>{{ product.name }} — {{ product.orderedAmount }}</p>\n{{/each}}", "Produkty w zamówieniu", "Pętle", "Powtarza zawartość dla każdego produktu w zamówieniu"),
        new("{{#each order.packingMaterials}}\n    <p>{{ packingMaterial.name }} — {{ packingMaterial.amount }} {{ packingMaterial.unit }}</p>\n{{/each}}", "Materiały pakowe w zamówieniu", "Pętle", "Powtarza zawartość dla każdego materiału pakowego")
    ];
}
