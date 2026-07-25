namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

/// <summary>
/// Immutable snapshot of the current pagination state.
/// Contains no WPF dependencies and can be safely passed to a ViewModel.
/// </summary>
public readonly record struct PaginationState(int PageIndex, int PageSize);