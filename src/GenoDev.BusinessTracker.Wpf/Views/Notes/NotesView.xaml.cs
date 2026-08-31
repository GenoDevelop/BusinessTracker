using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetAll;
using GenoDev.BusinessTracker.Wpf.ViewModels.Notes;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Views.Notes;

public partial class NotesView : UserControl
{
    private NotesViewModel? _attachedViewModel;
    private bool _hasUnsavedChanges;
    private bool _isLoadingDocument;
    private bool _isResolvingNoteSelection;
    private bool _isSynchronizingNoteSelection;
    private bool _isUpdatingFormattingToolbar;

    public NotesView()
    {
        InitializeComponent();

        Loaded += NotesView_Loaded;
        Unloaded += NotesView_Unloaded;
        DataContextChanged += NotesView_DataContextChanged;
    }

    private void NotesView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as NotesViewModel);
    }

    private void NotesView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void NotesView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as NotesViewModel);
        }
    }

    private void AttachViewModel(NotesViewModel? viewModel)
    {
        if (ReferenceEquals(_attachedViewModel, viewModel))
        {
            return;
        }

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested -=
                ViewModel_PaginationRefreshRequested;
            _attachedViewModel.NoteContentReady -= ViewModel_NoteContentReady;
            _attachedViewModel.SelectionChangeApprovalRequested -=
                ViewModel_SelectionChangeApprovalRequested;
        }

        _attachedViewModel = viewModel;

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested +=
                ViewModel_PaginationRefreshRequested;
            _attachedViewModel.NoteContentReady += ViewModel_NoteContentReady;
            _attachedViewModel.SelectionChangeApprovalRequested +=
                ViewModel_SelectionChangeApprovalRequested;
            if (!_hasUnsavedChanges)
            {
                LoadDocument(_attachedViewModel.EditorContentRtf);
            }
        }
    }

    private async void ViewModel_PaginationRefreshRequested()
    {
        await NotesPagination.RefreshAsync();
    }

    private void ViewModel_NoteContentReady(string contentRtf)
    {
        LoadDocument(contentRtf);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await NotesPagination.RefreshAsync();
    }

    private async void FilterToggleButton_Click(object sender, RoutedEventArgs e)
    {
        await NotesPagination.RefreshAsync();
    }

    private async void SearchTerm_SourceUpdated(
        object sender,
        DataTransferEventArgs e)
    {
        await NotesPagination.RefreshAsync();
    }

    private async void NotesList_SelectionChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (_isSynchronizingNoteSelection || _isResolvingNoteSelection ||
            DataContext is not NotesViewModel viewModel)
        {
            return;
        }

        var requestedNote = NotesList.SelectedItem as NoteListItemDto;
        var currentNote = viewModel.SelectedNote;
        if (requestedNote?.Id == currentNote?.Id)
        {
            return;
        }

        // ObservableCollection.Clear() temporarily clears ListBox.SelectedItem.
        // The ViewModel restores the stable-ID selection after replacing the page.
        if (requestedNote is null && currentNote is not null && viewModel.Notes.Count == 0)
        {
            return;
        }

        _isResolvingNoteSelection = true;
        NotesList.IsEnabled = false;
        try
        {
            SynchronizeListSelection(currentNote);
            var selectionChanged = await viewModel.RequestSelectionChangeAsync(requestedNote);
            SynchronizeListSelection(viewModel.SelectedNote);
            if (!selectionChanged)
            {
                RestoreEditorFocusAndFormattingState();
            }
        }
        finally
        {
            NotesList.IsEnabled = true;
            _isResolvingNoteSelection = false;
        }
    }

    private async Task<bool> ViewModel_SelectionChangeApprovalRequested()
    {
        if (!_hasUnsavedChanges || _attachedViewModel?.SelectedNote is not { } currentNote)
        {
            return true;
        }

        var wasListEnabled = NotesList.IsEnabled;
        NotesList.IsEnabled = false;
        SynchronizeListSelection(currentNote);
        try
        {
            var decision = MessageBox.Show(
                $"Notatka „{currentNote.Name}” zawiera niezapisane zmiany. " +
                "Czy chcesz je zapisać przed przejściem do innej notatki?",
                "Niezapisane zmiany",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (decision == MessageBoxResult.Yes)
            {
                var saved = await SaveCurrentEditorContentAsync(_attachedViewModel);
                if (!saved)
                {
                    RestoreEditorFocusAndFormattingState();
                }

                return saved;
            }

            if (decision == MessageBoxResult.No)
            {
                _hasUnsavedChanges = false;
                return true;
            }

            RestoreEditorFocusAndFormattingState();
            return false;
        }
        finally
        {
            NotesList.IsEnabled = wasListEnabled;
        }
    }

    private void SynchronizeListSelection(NoteListItemDto? note)
    {
        _isSynchronizingNoteSelection = true;
        try
        {
            NotesList.SelectedItem = note;
        }
        finally
        {
            _isSynchronizingNoteSelection = false;
        }
    }

    private void FontSizeComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingFormattingToolbar || NoteEditor is null ||
            sender is not ComboBox { SelectedItem: double fontSize })
        {
            return;
        }

        NoteEditor.Selection.ApplyPropertyValue(
            TextElement.FontSizeProperty,
            fontSize);
        RestoreEditorFocusAndFormattingState();
    }

    private void TextColorComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingFormattingToolbar || NoteEditor is null ||
            sender is not ComboBox { SelectedItem: ComboBoxItem item } ||
            item.Tag is not string colorName)
        {
            return;
        }

        var brush = (Brush)new BrushConverter().ConvertFromString(colorName)!;
        NoteEditor.Selection.ApplyPropertyValue(
            TextElement.ForegroundProperty,
            brush);
        RestoreEditorFocusAndFormattingState();
    }

    private void FormattingToggleButton_Click(object sender, RoutedEventArgs e)
    {
        RestoreEditorFocusAndFormattingState();
    }

    private void SpellCheckToggleButton_Click(object sender, RoutedEventArgs e)
    {
        SpellCheck.SetIsEnabled(
            NoteEditor,
            SpellCheckToggleButton.IsChecked == true);
        RestoreEditorFocusAndFormattingState();
    }

    private void NoteEditor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateFormattingToolbar();
    }

    private void NoteEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoadingDocument && DataContext is NotesViewModel viewModel)
        {
            _hasUnsavedChanges = true;
            viewModel.MarkContentChanged();
            UpdateFormattingToolbar();
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NotesViewModel viewModel)
        {
            return;
        }

        await SaveCurrentEditorContentAsync(viewModel);
    }

    private async Task<bool> SaveCurrentEditorContentAsync(NotesViewModel viewModel)
    {
        var saved = await viewModel.SaveContentAsync(GetEditorContentRtf());
        if (saved)
        {
            _hasUnsavedChanges = false;
        }

        return saved;
    }

    private string GetEditorContentRtf()
    {
        var range = new TextRange(
            NoteEditor.Document.ContentStart,
            NoteEditor.Document.ContentEnd);
        using var stream = new MemoryStream();
        range.Save(stream, DataFormats.Rtf);
        return Encoding.Latin1.GetString(stream.ToArray());
    }

    private void LoadDocument(string contentRtf)
    {
        _isLoadingDocument = true;
        try
        {
            NoteEditor.Document = new FlowDocument();
            if (string.IsNullOrEmpty(contentRtf))
            {
                return;
            }

            var range = new TextRange(
                NoteEditor.Document.ContentStart,
                NoteEditor.Document.ContentEnd);
            using var stream = new MemoryStream(
                Encoding.Latin1.GetBytes(contentRtf));
            range.Load(stream, DataFormats.Rtf);
        }
        finally
        {
            _isLoadingDocument = false;
            _hasUnsavedChanges = false;
            UpdateFormattingToolbar();
        }
    }

    private void RestoreEditorFocusAndFormattingState()
    {
        Dispatcher.BeginInvoke(
            () =>
            {
                NoteEditor.Focus();
                UpdateFormattingToolbar();
            },
            DispatcherPriority.Input);
    }

    private void UpdateFormattingToolbar()
    {
        if (NoteEditor is null || BoldToggleButton is null)
        {
            return;
        }

        _isUpdatingFormattingToolbar = true;
        try
        {
            var selection = NoteEditor.Selection;

            BoldToggleButton.IsChecked =
                selection.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight fontWeight &&
                fontWeight == FontWeights.Bold;
            ItalicToggleButton.IsChecked =
                selection.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle fontStyle &&
                fontStyle == FontStyles.Italic;

            var decorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);
            UnderlineToggleButton.IsChecked =
                decorations is TextDecorationCollection textDecorations &&
                textDecorations.Any(decoration =>
                    decoration.Location == TextDecorationLocation.Underline);

            var alignment = selection.GetPropertyValue(Block.TextAlignmentProperty);
            AlignLeftToggleButton.IsChecked =
                alignment is TextAlignment.Left;
            AlignCenterToggleButton.IsChecked =
                alignment is TextAlignment.Center;
            AlignRightToggleButton.IsChecked =
                alignment is TextAlignment.Right;

            BulletsToggleButton.IsChecked =
                selection.Start.Paragraph?.Parent is ListItem;

            var fontSize = selection.GetPropertyValue(TextElement.FontSizeProperty);
            FontSizeComboBox.SelectedItem =
                fontSize is double currentFontSize &&
                FontSizeComboBox.Items.Cast<double>().Contains(currentFontSize)
                    ? currentFontSize
                    : null;

            var foreground = selection.GetPropertyValue(TextElement.ForegroundProperty);
            TextColorComboBox.SelectedItem = foreground is SolidColorBrush currentBrush
                ? TextColorComboBox.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(item =>
                        item.Tag is string colorName &&
                        new BrushConverter().ConvertFromString(colorName) is SolidColorBrush itemBrush &&
                        itemBrush.Color == currentBrush.Color)
                : null;
        }
        finally
        {
            _isUpdatingFormattingToolbar = false;
        }
    }
}
