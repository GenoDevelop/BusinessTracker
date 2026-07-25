namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

/// <summary>
/// Loads one page of data and returns the total number of matching records.
/// The implementation is responsible for assigning the loaded items to its target collection.
/// </summary>
public delegate Task<int> PaginationPageLoader(
    PaginationState state,
    CancellationToken cancellationToken);