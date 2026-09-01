using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailingWorkspace;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetResendComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.QueueOutgoingEmail;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

public partial class MailComposerViewModel(IMediator mediator, Guid sourceId, bool isResend = false) : ViewModelBase
{
    private Guid _orderId;
    private Guid? _resentFromEmailId;
    private Guid? _mailTemplateId;

    public event EventHandler? RequestClose;
    public ObservableCollection<SmtpAccountDto> Accounts { get; } = [];
    public ObservableCollection<MailTemplateDto> Templates { get; } = [];
    public ObservableCollection<MailAttachmentEditorItem> Attachments { get; } = [];
    public ObservableCollection<MailAttachmentDifferenceItem> Differences { get; } = [];
    public bool HasDifferences => Differences.Count > 0;
    public bool IsResend => isResend;

    [ObservableProperty] private SmtpAccountDto? _selectedAccount;
    [ObservableProperty] private MailTemplateDto? _selectedTemplate;
    [ObservableProperty] private string _recipientAddress = string.Empty;
    [ObservableProperty] private string? _recipientName;
    [ObservableProperty] private string _subject = string.Empty;
    [ObservableProperty] private string _htmlBody = "<p></p>";
    [ObservableProperty] private string _templateDisplayName = "Bez szablonu";
    [ObservableProperty] private string? _attachmentErrorMessage;

    public async Task InitializeAsync()
    {
        try
        {
            var workspace = await mediator.Send(new GetMailingWorkspaceQuery());
            Replace(Accounts, workspace.Accounts.Where(x => x.IsEnabled));
            Replace(Templates, workspace.Templates.Where(x => x.IsActive));
            if (isResend)
            {
                var data = await mediator.Send(new GetResendComposerQuery(sourceId));
                _orderId = data.OrderId; _resentFromEmailId = data.OriginalEmailId; _mailTemplateId = data.MailTemplateId;
                RecipientAddress = data.RecipientAddress; RecipientName = data.RecipientName;
                SelectedAccount = Accounts.FirstOrDefault(x => x.Id == data.SmtpAccountId);
                SelectedTemplate = Templates.FirstOrDefault(x => x.Id == data.MailTemplateId);
                TemplateDisplayName = data.MailTemplateName ?? "Bez szablonu";
                Subject = data.Subject; HtmlBody = data.HtmlBody;
                Replace(Differences, data.Differences.Select(x => new MailAttachmentDifferenceItem(x)));
                var templateIds = SelectedTemplate?.Attachments.Select(x => x.Id).ToHashSet() ?? [];
                var pendingCurrentIds = data.Differences.Where(x => x.CurrentAttachment is not null)
                    .Select(x => x.CurrentAttachment!.Id).ToHashSet();
                foreach (var item in data.AvailableAttachments.Where(x => x.Content is not null && !pendingCurrentIds.Contains(x.Id)))
                    Attachments.Add(ToEditor(item, templateIds.Contains(item.Id) ? item.Id : null));
                NotifyDifferencesChanged();
            }
            else
            {
                _orderId = sourceId;
                await LoadComposerAsync(null);
            }
        }
        catch (RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
        }
    }

    [RelayCommand]
    private async Task ApplySelectedTemplate()
    {
        if (isResend) return;
        try { await LoadComposerAsync(SelectedTemplate?.Id); }
        catch (RequestValidationException exception) { ApplyValidationErrors(exception); }
    }

    private async Task LoadComposerAsync(Guid? templateId)
    {
        var data = await mediator.Send(new GetMailComposerQuery(_orderId, templateId));
        _mailTemplateId = data.MailTemplateId;
        AttachmentErrorMessage = null;
        RecipientAddress = data.RecipientAddress; RecipientName = data.RecipientName;
        SelectedAccount = Accounts.FirstOrDefault(x => x.Id == data.SmtpAccountId);
        Subject = data.Subject; HtmlBody = data.HtmlBody;
        Attachments.Clear();
        foreach (var item in data.Attachments.Where(x => x.Content is not null)) Attachments.Add(ToEditor(item, item.Id));
    }

    [RelayCommand] private async Task AddAttachments()
    {
        var filePaths = MailFileHelpers.SelectAttachmentFiles("Dodaj załączniki do wiadomości");
        if (filePaths is null) return;

        AttachmentErrorMessage = null;
        IsBusy = true;
        try
        {
            var loadedAttachments = await MailFileHelpers.LoadAttachmentsAsync(Attachments, filePaths);
            foreach (var attachment in loadedAttachments)
            {
                Attachments.Add(attachment);
            }
        }
        catch (RequestValidationException exception)
        {
            AttachmentErrorMessage = string.Join(
                Environment.NewLine,
                exception.Errors.Select(error => error.Message).Distinct());
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private void RemoveAttachment(MailAttachmentEditorItem? item)
    {
        if (item is not null) Attachments.Remove(item);
    }

    [RelayCommand]
    private async Task DownloadAttachment(MailAttachmentEditorItem? item)
    {
        if (item is null) return;
        AttachmentErrorMessage = null;
        try
        {
            await MailFileHelpers.SaveAttachmentAsync(item.FileName, item.Content);
        }
        catch (RequestValidationException exception)
        {
            AttachmentErrorMessage = string.Join(Environment.NewLine, exception.Errors.Select(x => x.Message).Distinct());
        }
    }

    [RelayCommand]
    private async Task DownloadDifference(MailAttachmentDifferenceItem? difference)
    {
        if (difference?.CurrentAttachment is not { Content: { } content } current) return;
        AttachmentErrorMessage = null;
        try
        {
            await MailFileHelpers.SaveAttachmentAsync(current.FileName, content);
        }
        catch (RequestValidationException exception)
        {
            AttachmentErrorMessage = string.Join(Environment.NewLine, exception.Errors.Select(x => x.Message).Distinct());
        }
    }

    [RelayCommand]
    private void RemoveDifference(MailAttachmentDifferenceItem? difference)
    {
        if (difference is null) return;
        RemoveCurrentAttachment(difference);
        Differences.Remove(difference);
        NotifyDifferencesChanged();
    }

    [RelayCommand]
    private void AcceptDifference(MailAttachmentDifferenceItem? difference)
    {
        if (difference?.CurrentAttachment is not { Content: not null } current) return;
        if (FindCurrentAttachment(difference) is null)
        {
            Attachments.Add(ToEditor(current, current.Id));
        }

        Differences.Remove(difference);
        NotifyDifferencesChanged();
    }

    [RelayCommand]
    private async Task ReplaceDifference(MailAttachmentDifferenceItem? difference)
    {
        if (difference is null) return;
        var filePaths = MailFileHelpers.SelectAttachmentFiles($"Podmień załącznik „{difference.FileName}”", multiselect: false);
        if (filePaths is null) return;

        AttachmentErrorMessage = null;
        IsBusy = true;
        try
        {
            var current = FindCurrentAttachment(difference);
            var attachmentsForValidation = current is null ? Attachments.ToList() : Attachments.Where(x => !ReferenceEquals(x, current)).ToList();
            var replacement = (await MailFileHelpers.LoadAttachmentsAsync(attachmentsForValidation, filePaths)).Single();
            var insertionIndex = current is null ? Attachments.Count : Attachments.IndexOf(current);
            if (current is not null) Attachments.Remove(current);
            Attachments.Insert(insertionIndex, replacement);
            Differences.Remove(difference);
            NotifyDifferencesChanged();
        }
        catch (RequestValidationException exception)
        {
            AttachmentErrorMessage = string.Join(Environment.NewLine, exception.Errors.Select(x => x.Message).Distinct());
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSend() => !HasDifferences;

    [RelayCommand(CanExecute = nameof(CanSend))] private async Task Send()
    {
        ClearValidationErrors();
        if (HasDifferences)
        {
            ApplyValidationErrors(RequestValidationException.For("Zaakceptuj, usuń lub podmień wszystkie problematyczne załączniki przed ponowną wysyłką."));
            return;
        }
        if (SelectedAccount is null)
        {
            ApplyValidationErrors(RequestValidationException.For("Wybierz konto SMTP."));
            return;
        }

        IsBusy = true;
        try
        {
            var inputs = Attachments.Select(x => new MailAttachmentInput(x.Id, x.FileName, x.ContentType, x.Content, x.TemplateAttachmentId)).ToList();
            await mediator.Send(new QueueOutgoingEmailCommand(_orderId, SelectedAccount.Id, _mailTemplateId, _resentFromEmailId,
                RecipientAddress, RecipientName, Subject, HtmlBody, inputs));
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (RequestValidationException exception) { ApplyValidationErrors(exception); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void Close() => RequestClose?.Invoke(this, EventArgs.Empty);

    private static MailAttachmentEditorItem ToEditor(MailTemplateAttachmentDto item, Guid? templateAttachmentId) =>
        new() { Id = null, TemplateAttachmentId = templateAttachmentId, FileName = item.FileName, ContentType = item.ContentType,
            Content = item.Content!, Sha256 = item.Sha256 };

    private MailAttachmentEditorItem? FindCurrentAttachment(MailAttachmentDifferenceItem difference) =>
        difference.CurrentAttachment is not { } current
            ? null
            : Attachments.FirstOrDefault(x => x.TemplateAttachmentId == current.Id || x.Id == current.Id);

    private void RemoveCurrentAttachment(MailAttachmentDifferenceItem difference)
    {
        if (FindCurrentAttachment(difference) is { } current) Attachments.Remove(current);
    }

    private void NotifyDifferencesChanged()
    {
        OnPropertyChanged(nameof(HasDifferences));
        SendCommand.NotifyCanExecuteChanged();
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear(); foreach (var value in values) target.Add(value);
    }
}
