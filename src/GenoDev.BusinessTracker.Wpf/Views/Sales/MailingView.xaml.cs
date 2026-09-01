using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

namespace GenoDev.BusinessTracker.Wpf.Views.Sales;

public partial class MailingView : UserControl
{
    private TextBox? _lastEditor;
    private MailingViewModel? _viewModel;

    public MailingView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel();
    }

    private async void MailingView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel();
        if (_viewModel is not null) await _viewModel.LoadAsync();
        await HistoryPagination.RefreshAsync();
    }

    private void MailingView_Unloaded(object sender, RoutedEventArgs e) => DetachViewModel();

    private void AttachViewModel()
    {
        if (ReferenceEquals(_viewModel, DataContext)) return;
        DetachViewModel();
        _viewModel = DataContext as MailingViewModel;
        if (_viewModel is not null) _viewModel.HistoryRefreshRequested += OnHistoryRefreshRequested;
    }

    private void DetachViewModel()
    {
        if (_viewModel is not null) _viewModel.HistoryRefreshRequested -= OnHistoryRefreshRequested;
        _viewModel = null;
    }

    private async void OnHistoryRefreshRequested() => await HistoryPagination.RefreshAsync();
    private async void HistoryRefreshButton_Click(object sender, RoutedEventArgs e) => await HistoryPagination.RefreshAsync();
    private void Editor_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => _lastEditor = sender as TextBox;

    private void TokenList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as ListBox)?.SelectedItem is not MailTokenDto token) return;
        if (ReferenceEquals(_lastEditor, SubjectTemplateEditor) && token.Group != "Zmienne") return;

        InsertAtCaret(token.Token);
    }

    private void SnippetList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as ListBox)?.SelectedItem is MailSnippetDto snippet &&
            (ReferenceEquals(_lastEditor, HtmlTemplateEditor) || ReferenceEquals(_lastEditor, SnippetHtmlEditor)))
            InsertAtCaret($"{{{{> {snippet.Key} }}}}");
    }

    private void InsertAtCaret(string value)
    {
        if (_lastEditor is null) return;
        var start = _lastEditor.SelectionStart;
        _lastEditor.Text = _lastEditor.Text.Insert(start, value);
        _lastEditor.SelectionStart = start + value.Length;
        _lastEditor.Focus();
    }

    private void SmtpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MailingViewModel vm && sender is PasswordBox box) vm.SmtpPassword = box.Password;
    }
}
