using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetDetails;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.UpdateContent;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Notes;

public partial class NotesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _detailsCancellation;
    private Guid? _loadedNoteId;
    private Guid? _pendingCreatedNoteId;
    private long _detailsRequestVersion;

    public NotesViewModel(
        IMediator mediator,
        IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        CreateNoteCommand = new RelayCommand(OpenCreatePopup);
    }

    public ObservableCollection<NoteListItemDto> Notes { get; } = new();

    public PaginationPageLoader NotesPageLoader => LoadNotesPageAsync;

    public IRelayCommand CreateNoteCommand { get; }

    public event Action? PaginationRefreshRequested;
    public event Action<string>? NoteContentReady;
    public event Func<Task<bool>>? SelectionChangeApprovalRequested;

    [ObservableProperty]
    private NoteListItemDto? _selectedNote;

    [ObservableProperty]
    private string? _editorNoteName;

    [ObservableProperty]
    private string _editorContentRtf = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditorEnabled))]
    private bool _isEditorLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditorEnabled))]
    private bool _isSavingContent;

    [ObservableProperty]
    private string? _saveStatus;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateNoteViewModel? _createNoteViewModel;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private NoteSortBy _sortBy = NoteSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    public bool IsEditorEnabled =>
        _loadedNoteId.HasValue && !IsEditorLoading && !IsSavingContent;

    public void SetSorting(NoteSortBy sortBy, bool isDescending)
    {
        SortBy = sortBy;
        IsDescending = isDescending;
    }

    public void MarkContentChanged()
    {
        SaveStatus = null;
    }

    public async Task<bool> RequestSelectionChangeAsync(NoteListItemDto? note)
    {
        if (note?.Id == SelectedNote?.Id)
        {
            SelectedNote = note;
            return true;
        }

        var approval = SelectionChangeApprovalRequested;
        if (approval is not null && !await approval())
        {
            return false;
        }

        SelectedNote = note;
        return true;
    }

    public async Task<bool> SaveContentAsync(string contentRtf)
    {
        if (!_loadedNoteId.HasValue || IsEditorLoading || IsSavingContent)
        {
            return false;
        }

        ClearValidationErrors();
        var noteId = _loadedNoteId.Value;
        IsSavingContent = true;
        SaveStatus = null;
        try
        {
            await _mediator.Send(
                new UpdateNoteContentCommand(noteId, contentRtf));
            if (_loadedNoteId == noteId)
            {
                EditorContentRtf = contentRtf;
                SaveStatus = "Zapisano.";
            }
            return true;
        }
        catch (RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
            return false;
        }
        finally
        {
            IsSavingContent = false;
        }
    }

    partial void OnSelectedNoteChanged(NoteListItemDto? value)
    {
        if (value?.Id == _loadedNoteId)
        {
            return;
        }

        BeginLoadSelectedNote(value);
    }

    private async Task<int> LoadNotesPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var previousSelection = SelectedNote;
        var previousSelectionId = previousSelection?.Id;
        var result = await _mediator.Send(
            new GetNotesQuery(
                state.PageIndex,
                state.PageSize,
                SortBy,
                IsDescending,
                IsFilterVisible ? SearchTerm : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var requestedSelection = ReplaceItemsPreservingSelection(
            Notes,
            result.Items,
            previousSelection,
            note => note.Id,
            _pendingCreatedNoteId);
        _pendingCreatedNoteId = null;

        if (!await RequestSelectionChangeAsync(requestedSelection))
        {
            SelectedNote = previousSelectionId.HasValue
                ? Notes.FirstOrDefault(note => note.Id == previousSelectionId.Value) ?? previousSelection
                : null;
        }

        return result.TotalCount;
    }

    private async void BeginLoadSelectedNote(NoteListItemDto? selectedNote)
    {
        await LoadSelectedNoteAsync(selectedNote);
    }

    private async Task LoadSelectedNoteAsync(NoteListItemDto? selectedNote)
    {
        var requestVersion = ++_detailsRequestVersion;
        var cancellation = new CancellationTokenSource();
        var previousCancellation = _detailsCancellation;
        _detailsCancellation = cancellation;
        previousCancellation?.Cancel();
        previousCancellation?.Dispose();

        if (selectedNote is null)
        {
            _loadedNoteId = null;
            EditorNoteName = null;
            EditorContentRtf = string.Empty;
            SaveStatus = null;
            NoteContentReady?.Invoke(EditorContentRtf);
            OnPropertyChanged(nameof(IsEditorEnabled));
            cancellation.Dispose();
            if (ReferenceEquals(_detailsCancellation, cancellation))
            {
                _detailsCancellation = null;
            }
            return;
        }

        IsEditorLoading = true;
        SaveStatus = null;
        try
        {
            var details = await _mediator.Send(
                new GetNoteDetailsQuery(selectedNote.Id),
                cancellation.Token);

            if (requestVersion != _detailsRequestVersion)
            {
                return;
            }

            _loadedNoteId = details.Id;
            EditorNoteName = details.Name;
            EditorContentRtf = details.ContentRtf;
            NoteContentReady?.Invoke(EditorContentRtf);
            OnPropertyChanged(nameof(IsEditorEnabled));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (RequestValidationException exception)
        {
            if (requestVersion == _detailsRequestVersion)
            {
                ApplyValidationErrors(exception);
            }
        }
        finally
        {
            if (requestVersion == _detailsRequestVersion)
            {
                IsEditorLoading = false;
                _detailsCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void OpenCreatePopup()
    {
        var editor = _serviceProvider.GetRequiredService<CreateNoteViewModel>();
        editor.Clear();
        editor.RequestClose += result =>
        {
            IsCreatePopupOpen = false;
            if (result.RequiresRefresh)
            {
                _pendingCreatedNoteId = result.CreatedEntityId;
                PaginationRefreshRequested?.Invoke();
            }
        };

        CreateNoteViewModel = editor;
        IsCreatePopupOpen = true;
        RequestPopupOpen(nameof(IsCreatePopupOpen));
    }
}
