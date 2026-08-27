using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels.Products;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public partial class ProductImagesPopup : UserControl
{
    private ProductImagesViewModel? _viewModel;
    private ProductImagesWindow? _window;

    public ProductImagesPopup()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        AttachViewModel(DataContext as ProductImagesViewModel);

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.IsPopupOpen == true)
        {
            _viewModel.ClosePopupCommand.Execute(null);
        }

        AttachViewModel(null);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as ProductImagesViewModel);
        }
    }

    private void AttachViewModel(ProductImagesViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            SynchronizeWindow();
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        CloseWindow();
        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        SynchronizeWindow();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductImagesViewModel.IsPopupOpen))
        {
            SynchronizeWindow();
        }
    }

    private void SynchronizeWindow()
    {
        if (_viewModel?.IsPopupOpen == true && IsLoaded)
        {
            if (_window is not null)
            {
                return;
            }

            var hostWindow = Window.GetWindow(this);
            _window = new ProductImagesWindow
            {
                DataContext = _viewModel,
                HostWindow = hostWindow
            };
            CenterWindowOnHost(_window, hostWindow);
            _window.Closed += OnWindowClosed;
            _window.Show();
            return;
        }

        CloseWindow();
    }

    private static void CenterWindowOnHost(ProductImagesWindow window, Window? hostWindow)
    {
        if (hostWindow is not { IsVisible: true })
        {
            var workArea = SystemParameters.WorkArea;
            window.Left = workArea.Left + (workArea.Width - window.Width) / 2;
            window.Top = workArea.Top + (workArea.Height - window.Height) / 2;
            return;
        }

        window.Left = hostWindow.Left + (hostWindow.ActualWidth - window.Width) / 2;
        window.Top = hostWindow.Top + (hostWindow.ActualHeight - window.Height) / 2;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_window is not null)
        {
            _window.Closed -= OnWindowClosed;
            _window = null;
        }

        if (_viewModel?.IsPopupOpen == true)
        {
            _viewModel.ClosePopupCommand.Execute(null);
        }
    }

    private void CloseWindow()
    {
        if (_window is null)
        {
            return;
        }

        var window = _window;
        _window = null;
        window.Closed -= OnWindowClosed;
        window.Close();
    }
}
