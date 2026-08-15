using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.Create;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Notes;

public partial class CreateNoteViewModel(IMediator mediator) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    public event Action<EditorCloseResult>? RequestClose;

    public void Clear()
    {
        ClearValidationErrors();
        Name = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        ClearValidationErrors();
        IsBusy = true;
        try
        {
            var createdNoteId = await mediator.Send(new CreateNoteCommand(Name));
            RequestClose?.Invoke(EditorCloseResult.Saved(createdNoteId));
        }
        catch (RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(EditorCloseResult.Cancelled);
    }
}
