using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class SideNavigationSettings : DependencyObject
{
    public SideNavigationSettings()
    {
        ToggleCompactModeCommand = new RelayCommand(() => IsCompact = !IsCompact);
    }

    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact),
        typeof(bool),
        typeof(SideNavigationSettings),
        new PropertyMetadata(false));

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public IRelayCommand ToggleCompactModeCommand { get; }
}
