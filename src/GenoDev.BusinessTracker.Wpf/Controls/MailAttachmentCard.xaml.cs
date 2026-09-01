using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class MailAttachmentCard : UserControl
{
    public static readonly DependencyProperty RemoveCommandProperty = DependencyProperty.Register(
        nameof(RemoveCommand), typeof(ICommand), typeof(MailAttachmentCard));

    public static readonly DependencyProperty ReplaceCommandProperty = DependencyProperty.Register(
        nameof(ReplaceCommand), typeof(ICommand), typeof(MailAttachmentCard));

    public static readonly DependencyProperty IsReplaceAvailableProperty = DependencyProperty.Register(
        nameof(IsReplaceAvailable), typeof(bool), typeof(MailAttachmentCard), new PropertyMetadata(false));

    public static readonly DependencyProperty AcceptCommandProperty = DependencyProperty.Register(
        nameof(AcceptCommand), typeof(ICommand), typeof(MailAttachmentCard));

    public static readonly DependencyProperty IsAcceptAvailableProperty = DependencyProperty.Register(
        nameof(IsAcceptAvailable), typeof(bool), typeof(MailAttachmentCard), new PropertyMetadata(false));

    public static readonly DependencyProperty DownloadCommandProperty = DependencyProperty.Register(
        nameof(DownloadCommand), typeof(ICommand), typeof(MailAttachmentCard));

    public static readonly DependencyProperty IsDownloadAvailableProperty = DependencyProperty.Register(
        nameof(IsDownloadAvailable), typeof(bool), typeof(MailAttachmentCard), new PropertyMetadata(false));

    public static readonly DependencyProperty IsWarningProperty = DependencyProperty.Register(
        nameof(IsWarning), typeof(bool), typeof(MailAttachmentCard), new PropertyMetadata(false));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(MailAttachmentCard));

    public MailAttachmentCard() => InitializeComponent();

    public ICommand? RemoveCommand
    {
        get => (ICommand?)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    public ICommand? ReplaceCommand
    {
        get => (ICommand?)GetValue(ReplaceCommandProperty);
        set => SetValue(ReplaceCommandProperty, value);
    }

    public bool IsReplaceAvailable
    {
        get => (bool)GetValue(IsReplaceAvailableProperty);
        set => SetValue(IsReplaceAvailableProperty, value);
    }

    public ICommand? AcceptCommand
    {
        get => (ICommand?)GetValue(AcceptCommandProperty);
        set => SetValue(AcceptCommandProperty, value);
    }

    public bool IsAcceptAvailable
    {
        get => (bool)GetValue(IsAcceptAvailableProperty);
        set => SetValue(IsAcceptAvailableProperty, value);
    }

    public ICommand? DownloadCommand
    {
        get => (ICommand?)GetValue(DownloadCommandProperty);
        set => SetValue(DownloadCommandProperty, value);
    }

    public bool IsDownloadAvailable
    {
        get => (bool)GetValue(IsDownloadAvailableProperty);
        set => SetValue(IsDownloadAvailableProperty, value);
    }

    public bool IsWarning
    {
        get => (bool)GetValue(IsWarningProperty);
        set => SetValue(IsWarningProperty, value);
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
