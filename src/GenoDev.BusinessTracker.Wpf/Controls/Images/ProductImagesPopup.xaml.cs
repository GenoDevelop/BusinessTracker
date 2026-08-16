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

            _window = new ProductImagesWindow
            {
                DataContext = _viewModel,
                Owner = Window.GetWindow(this)
            };
            _window.Closed += OnWindowClosed;
            _window.Show();
            return;
        }

        CloseWindow();
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
